using DBContexts.OIDC_Management.Entities;
using Microsoft.AspNet.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.WebUtilities;
using OIDCDemo.AuthorizationServer;
using OIDCDemo.AuthorizationServer.Helpers;
using OIDCDemo.AuthorizationServer.Models;
using Renci.SshNet.Security;
using Services.OIDC_Management.Executes;
using Services.OIDC_Management.Executes.AuthorizationClient;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// 1. Database Context
builder.Services.AddDbContext<oidcIdentityContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. Cấu hình Authentication
builder.Services.AddAuthentication("Cookies")
    .AddCookie("Cookies", options =>
    {
        options.LoginPath = "/Account/SignIn";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(1);
        options.SlidingExpiration = true;
    })
    .AddMicrosoftAccount(options =>
    {
        // --- Cấu hình Client ID/Secret ---
        options.ClientId = builder.Configuration["Authentication:Microsoft:ClientId"] ?? "YOUR_CLIENT_ID";
        options.ClientSecret = builder.Configuration["Authentication:Microsoft:ClientSecret"] ?? "YOUR_CLIENT_SECRET";

        // --- Code Nhóm 3: Lưu token & Xử lý lỗi ---
        options.SaveTokens = true;
        options.Events.OnRemoteFailure = context => {
            context.Response.Redirect("/Account/Login?error=" + context.Failure.Message);
            context.HandleResponse();
            return Task.CompletedTask;
        };

        // --- Logic Tenant (Giữ nguyên của code cũ) ---
        var tenantId = builder.Configuration["Authentication:Microsoft:TenantId"];
        if (!string.IsNullOrEmpty(tenantId) && tenantId != "YOUR_TENANT_ID")
        {
            options.AuthorizationEndpoint = $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/authorize";
            options.TokenEndpoint = $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token";
        }
    });

// 3. Services & DI
builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<ICodeStorage>(services => new MemoryCodeStorage());
builder.Services.AddSingleton<IRefreshTokenStorageFactory>(services => new MemoryRefreshTokenStorageFactory());

var tokenIssuingOptions = builder.Configuration.GetSection("TokenIssuing").Get<TokenIssuingOptions>() ?? new TokenIssuingOptions();

builder.Services.AddSingleton(tokenIssuingOptions);
builder.Services.AddSingleton(JwkLoader.LoadFromDefault());
builder.Services.AddScoped<AuthorizationClientModel>();
builder.Services.AddScoped<AuthorizationClientOne>();
builder.Services.AddScoped<ClientCommand>();
builder.Services.AddScoped<ClientMany>();
builder.Services.AddScoped<ClientOne>();
builder.Services.AddScoped<ClientModel>();
builder.Services.AddScoped<UserCommand>();
builder.Services.AddScoped<UserMany>();
builder.Services.AddScoped<UserOne>();
builder.Services.AddScoped<UserModel>();
builder.Services.AddScoped<AccountModel>();
builder.Services.AddScoped<AccountCommand>();
builder.Services.AddScoped<PasswordHasher>();

// 4. HttpContext & SSL Handler
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient("IgnoreSSL")
    .ConfigurePrimaryHttpMessageHandler(() =>
    {
        return new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
    });

// Đăng ký dịch vụ Identity để hệ thống tạo ra SignInManager và UserManager
// Lưu ý: Dùng đúng tên class AspNetUser và AspNetRole như trong hình
builder.Services.AddIdentity<AspNetUser, AspNetRole>(options =>
{
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<oidcIdentityContext>()
.AddDefaultTokenProviders();

// 5. RSA Key Generation
RSA rsa = RSA.Create(2048);
var rsaKey = new RsaSecurityKey(rsa)
{
    KeyId = Guid.NewGuid().ToString()
};
builder.Services.AddSingleton<RsaSecurityKey>(rsaKey);

// ================== APP BUILD ==================
var app = builder.Build();

app.MapGet("/.well-known/openid-configuration", () =>
{
    return Results.File(Path.Combine(builder.Environment.ContentRootPath, "oidc-assets", ".well-known/openid-configuration.json"), contentType: "application/json");
});

app.MapGet("/.well-known/jwks.json", () =>
{
    return Results.File(Path.Combine(builder.Environment.ContentRootPath, "oidc-assets", ".well-known/jwks.json"), contentType: "application/json");
});

// ================== REVOCATION ENDPOINT ==================
app.MapPost("/connect/revocation", async (
    HttpContext context,
    ClientOne clientRepository,
    IRefreshTokenStorageFactory refreshTokenFactory) =>
{
    var form = await context.Request.ReadFormAsync();
    var token = form["token"].ToString();
    var tokenTypeHint = form["token_type_hint"].ToString();
    var clientId = form["client_id"].ToString();
    var clientSecret = form["client_secret"].ToString();

    if (string.IsNullOrEmpty(token))
        return Results.BadRequest(Error("invalid_request", "token is required"));

    var authResult = await AuthenticateClientAsync(context, clientRepository);
    if (!authResult.IsAuthenticated)
        return Results.Unauthorized();

    var client = authResult.Client!;
    if (!string.IsNullOrEmpty(clientId) && clientId != client.ClientId)
        return Results.Unauthorized();

    if (string.IsNullOrEmpty(tokenTypeHint) || tokenTypeHint == "refresh_token")
    {
        var storage = refreshTokenFactory.GetInvalidatedTokenStorage();
        storage.TryAddToken(token);
    }
    return Results.Ok();
});

// ================== END SESSION ENDPOINT ==================
app.MapGet("/connect/endsession", async (
    HttpContext context,
    ClientOne clientRepository,
    IHttpContextAccessor httpContextAccessor) =>
{
    var idTokenHint = context.Request.Query["id_token_hint"].ToString();
    var postLogoutRedirectUri = context.Request.Query["post_logout_redirect_uri"].ToString();
    var state = context.Request.Query["state"].ToString();

    try { await context.SignOutAsync("Cookies"); } catch { /* ignore */ }

    string? clientIdFromToken = null;
    if (!string.IsNullOrEmpty(idTokenHint))
    {
        var validationResult = ValidateIdToken(idTokenHint, context.RequestServices);
        if (validationResult.IsValid)
        {
            var claims = validationResult.ClaimsPrincipal;
            clientIdFromToken = claims?.FindFirst("client_id")?.Value ?? claims?.FindFirst("aud")?.Value;
        }
    }

    var clientId = clientIdFromToken ?? context.Request.Query["client_id"].ToString();
    Client? client = null;
    if (!string.IsNullOrEmpty(clientId))
    {
        client = await clientRepository.GetByClientIdAsync(clientId);
    }

    var issuer = builder.Configuration["TokenIssuing:Issuer"];
    var baseIssuer = !string.IsNullOrEmpty(issuer) ? issuer.TrimEnd('/') : $"{context.Request.Scheme}://{context.Request.Host}";
    var authorizeEndpoint = $"{baseIssuer}/authorize";

    if (client != null)
    {
        string? redirectUri = context.Request.Query["redirect_uri"].ToString();
        if (string.IsNullOrWhiteSpace(redirectUri))
        {
            if (!string.IsNullOrWhiteSpace(client.CallbackPath))
                redirectUri = client.CallbackPath;
            else if (!string.IsNullOrWhiteSpace(client.RedirectUris))
                redirectUri = client.RedirectUris.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        }

        var queryParams = new Dictionary<string, string?>();
        queryParams["client_id"] = client.ClientId;
        if (!string.IsNullOrWhiteSpace(redirectUri)) queryParams["redirect_uri"] = redirectUri;
        queryParams["response_type"] = "code";
        queryParams["scope"] = string.IsNullOrWhiteSpace(client.Scope) ? "openid profile email" : client.Scope;

        var codeChallenge = context.Request.Query["code_challenge"].ToString();
        if (!string.IsNullOrEmpty(codeChallenge)) queryParams["code_challenge"] = codeChallenge;
        var codeChallengeMethod = context.Request.Query["code_challenge_method"].ToString();
        if (!string.IsNullOrEmpty(codeChallengeMethod)) queryParams["code_challenge_method"] = codeChallengeMethod;
        var nonce = context.Request.Query["nonce"].ToString();
        if (!string.IsNullOrEmpty(nonce)) queryParams["nonce"] = nonce;
        if (!string.IsNullOrEmpty(state)) queryParams["state"] = state;

        var finalParams = queryParams.Where(kvp => !string.IsNullOrEmpty(kvp.Value)).ToDictionary(kvp => kvp.Key, kvp => kvp.Value!);
        var finalUrl = QueryHelpers.AddQueryString(authorizeEndpoint, finalParams);

        return Results.Redirect(finalUrl);
    }

    if (!string.IsNullOrEmpty(postLogoutRedirectUri))
    {
        var sep = postLogoutRedirectUri.Contains('?') ? "&" : "?";
        var redirect = string.IsNullOrEmpty(state)
            ? postLogoutRedirectUri
            : $"{postLogoutRedirectUri}{sep}state={Uri.EscapeDataString(state)}";
        return Results.Redirect(redirect);
    }

    var fallback = "/";
    if (!string.IsNullOrEmpty(state))
        fallback = $"{fallback}{(fallback.Contains('?') ? "&" : "?")}state={Uri.EscapeDataString(state)}";

    return Results.Redirect(fallback);
});

// ================== HELPER FUNCTIONS ==================

async Task<(bool IsAuthenticated, Client? Client)> AuthenticateClientAsync(HttpContext context, ClientOne clientRepo)
{
    string? clientId = null;
    string? clientSecret = null;

    if (context.Request.Headers.Authorization.Count > 0 &&
        context.Request.Headers.Authorization[0].StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
    {
        var header = context.Request.Headers.Authorization[0].Substring("Basic ".Length).Trim();
        try
        {
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(header));
            var parts = decoded.Split(':');
            clientId = parts[0];
            clientSecret = parts.Length > 1 ? parts[1] : null;
        }
        catch { return (false, null); }
    }
    else
    {
        var form = await context.Request.ReadFormAsync();
        clientId = form["client_id"].ToString();
        clientSecret = form["client_secret"].ToString();
    }

    if (string.IsNullOrEmpty(clientId)) return (false, null);

    var client = await clientRepo.GetByClientIdAsync(clientId);
    if (client == null || client.ClientSecret != clientSecret)
        return (false, null);

    return (true, client);
}

(bool IsValid, ClaimsPrincipal ClaimsPrincipal) ValidateIdToken(string idToken, IServiceProvider services)
{
    try
    {
        var rsaKey = services.GetRequiredService<RsaSecurityKey>();
        var validationParameters = new TokenValidationParameters
        {
            ValidateAudience = false,
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["TokenIssuing:Issuer"] ?? "https://localhost:7000",
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = rsaKey,
            ValidateLifetime = true
        };

        var handler = new JsonWebTokenHandler();
        var result = handler.ValidateToken(idToken, validationParameters);

        var principal = result.ClaimsIdentity != null
            ? new ClaimsPrincipal(result.ClaimsIdentity)
            : new ClaimsPrincipal();

        return (result.IsValid, principal);
    }
    catch
    {
        return (false, new ClaimsPrincipal());
    }
}

IResult Error(string error, string description) =>
    Results.Json(new { error, error_description = description }, statusCode: 400);

// ================== MIDDLEWARE PIPELINE ==================
app.UseRouting();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();

// Routes
app.MapControllerRoute(
    name: "areas",
    pattern: "{controller=Home}/{action=Index}/{id?}",
    defaults: new { area = "Admin", controller = "Home", action = "Index" });

app.MapControllerRoute(
    name: "areas_generic",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=index}/{id?}");

app.Run();