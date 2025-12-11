using DBContexts.OIDC_Management.Entities;
using Microsoft.AspNet.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using NuGet.Common;
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
builder.Services.AddDbContext<oidcIdentityContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = "AdminCookies";
    options.DefaultSignInScheme = "AdminCookies";
    options.DefaultChallengeScheme = "SsoAuth";
})
    .AddCookie("AdminCookies", options =>
    {
        options.Cookie.Name = ".iteccom.Admin";
        options.LoginPath = "/Admin/Account/SignIn";
        options.AccessDeniedPath = "/Admin/Home/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(1);
        options.SlidingExpiration = true;
    })
    //.AddCookie("SsoAuth", options =>
    //{
    //    options.Cookie.Name = ".iteccom.Auth";
    //    options.Cookie.Domain = GetCookieDomain();                  // dev – có dấu chấm đầu
    //                                                                // options.Cookie.Domain = ".yourcompany.com";              // prod
    //    options.Cookie.HttpOnly = true;
    //    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    //    options.Cookie.SameSite = SameSiteMode.None;    // bắt buộc cho cross-subdomain
    //    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    //    options.SlidingExpiration = true;
    //    options.LoginPath = "/Authorize/Index";
    //})


    .AddCookie("SsoAuth", options =>
    {
        options.Cookie.Name = ".sso.Auth";
        // Không set Cookie.Domain để cookie hoạt động trên localhost
        // options.Cookie.Domain = ".bmwindows.vn";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.LoginPath = "/Authorize/Index";
    })

    .AddMicrosoftAccount("Microsoft", options =>
    {
        options.ClientId = builder.Configuration["Authentication:Microsoft:ClientId"] ?? "YOUR_CLIENT_ID";
        options.ClientSecret = builder.Configuration["Authentication:Microsoft:ClientSecret"] ?? "YOUR_CLIENT_SECRET";
        
        // CallbackPath - URL Microsoft sẽ redirect về sau khi đăng nhập
        options.CallbackPath = "/signin-microsoft";
        
        // SignInScheme - sau khi Microsoft auth xong, lưu vào cookie nào
        // Để trống hoặc dùng cookie tạm, AccountController.LoginCallback sẽ tự tạo SsoAuth
        options.SignInScheme = "AdminCookies";
        
        // TenantId = common cho phép mọi tài khoản Microsoft (cá nhân + tổ chức)
        var tenantId = builder.Configuration["Authentication:Microsoft:TenantId"] ?? "common";
        options.AuthorizationEndpoint = $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/authorize";
        options.TokenEndpoint = $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token";
        
        // Scope
        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add("email");
    });

builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
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
// ------------------ Authentication ------------------
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient("IgnoreSSL")
    .ConfigurePrimaryHttpMessageHandler(() =>
    {
        return new HttpClientHandler
        {
            // Bỏ qua mọi lỗi SSL (self-signed / mismatch / expired)
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
    });

RSA rsa = RSA.Create(2048); // tạo RSA 2048 bit

// Đóng gói thành RsaSecurityKey để dùng với JWT
var rsaKey = new RsaSecurityKey(rsa)
{
    KeyId = Guid.NewGuid().ToString() // mỗi key cần có kid
};
builder.Services.AddSingleton<RsaSecurityKey>(rsaKey);



var app = builder.Build();



app.MapGet("/.well-known/openid-configuration", () =>
{
    return Results.File(Path.Combine(builder.Environment.ContentRootPath, "oidc-assets", ".well-known/openid-configuration.json"), contentType: "application/json");
});

app.MapGet("/.well-known/jwks.json", () =>
{
    return Results.File(Path.Combine(builder.Environment.ContentRootPath, "oidc-assets", ".well-known/jwks.json"), contentType: "application/json");
});

// ================== 1. REVOCATION ENDPOINT (RFC 7009) ==================
app.MapPost("/connect/revocation", async (
    HttpContext context,
    ClientOne clientRepository,
    AuthorizationClientOne authOne,
    IRefreshTokenStorageFactory refreshTokenFactory) =>
{
    var form = await context.Request.ReadFormAsync();

    var token = form["token"].ToString();
    var tokenTypeHint = form["token_type_hint"].ToString();
    var clientId = form["client_id"].ToString();
    var clientSecret = form["client_secret"].ToString();

    if (string.IsNullOrEmpty(token))
        return Results.BadRequest(Error("invalid_request", "token is required"));

    // Authenticate client (supports both client_secret_post and client_secret_basic)
    var authResult = await AuthenticateClientAsync(context, clientRepository);
    if (!authResult.IsAuthenticated)
        return Results.Unauthorized();

    var client = authResult.Client!;
    if (!string.IsNullOrEmpty(clientId) && clientId != client.ClientId)
        return Results.Unauthorized();

    // Revoke refresh token if applicable
    if (string.IsNullOrEmpty(tokenTypeHint) || tokenTypeHint == "refresh_token")
    {
        // Sử dụng API hiện tại trong workspace:
        // IRefreshTokenStorageFactory cung cấp GetInvalidatedTokenStorage()
        // và IRefreshTokenStorage có TryAddToken/Contains.
        // Ở đây đánh dấu token là "invalidated" bằng cách thêm vào bộ lưu invalidated.
        var storage = refreshTokenFactory.GetInvalidatedTokenStorage();
        storage.TryAddToken(token);
    }
    // For access_token, do nothing (short-lived), still return 200 OK
    await authOne.RevokeTokenAsync(token);
    return Results.Ok(); // RFC 7009: always return 200 OK
});


// ================== . CHECK SESSION ENDPOINT ==================
 app.MapGet("/connect/check-session", async (
    [FromQuery] string sid,
    AuthorizationClientOne authOne) =>
{
    if (string.IsNullOrEmpty(sid))
        return Results.BadRequest("sid required");

    bool isValid = await authOne.CheckSessionAsync(sid);
    return Results.Text(isValid ? "valid" : "invalid");
});



// ================== 2. END SESSION ENDPOINT (OIDC Logout) ==================
app.MapGet("/connect/endsession", async (
    HttpContext context,
    ClientOne clientRepository,
    IHttpContextAccessor httpContextAccessor

    ) =>
{

    var idTokenHint = context.Request.Query["id_token_hint"].ToString();
    var postLogoutRedirectUri = context.Request.Query["post_logout_redirect_uri"].ToString();
    var state = context.Request.Query["state"].ToString();

    // 1. Sign out server-side properly
    //try { await context.SignOutAsync("SsoAuth"); } catch { /* ignore */ }
    // NEW: Always extract userId + sid from id_token_hint (because context.User is empty here)
    string? logoutUserId = null;
    string? logoutSid = null;

    if (!string.IsNullOrEmpty(idTokenHint))
    {
        var handler = new JsonWebTokenHandler();
        var token = handler.ReadJsonWebToken(idTokenHint);

        logoutUserId = token?.Claims?.FirstOrDefault(c => c.Type == "sub")?.Value;
        logoutSid = token?.Claims?.FirstOrDefault(c => c.Type == "sid")?.Value;
    }

    // Must have logoutUserId for logout
    if (string.IsNullOrEmpty(logoutUserId))
        return Results.BadRequest("Logout failed: userId missing from id_token_hint");

    // NOW deactivate correctly
    // MUST GET SESSIONS FIRST before deactivating
    List<UserSession> oldSessions;

    using (var scope = app.Services.CreateScope())
    {
        var authOne = scope.ServiceProvider.GetRequiredService<AuthorizationClientOne>();
        oldSessions = await authOne.GetSessionsForUserAsync(logoutUserId);

        await authOne.DeactivateAllSessionsForUserAsync(logoutUserId);
        Console.WriteLine($"[SSO LOGOUT] All sessions deactivated for user={logoutUserId}");
    }



    await context.SignOutAsync("SsoAuth");

    // 2. Try extract client_id from id_token_hint (nếu có)
    string? clientIdFromToken = null;
    if (!string.IsNullOrEmpty(idTokenHint))
    {
        var validationResult = ValidateIdToken(idTokenHint, context.RequestServices);
        if (validationResult.IsValid)
        {
            var claims = validationResult.ClaimsPrincipal;
            clientIdFromToken = claims?.FindFirst("client_id")?.Value
                                ?? claims?.FindFirst("aud")?.Value;
        }
    }

    // 3. Determine clientId (token -> query fallback)
    var clientId = clientIdFromToken ?? context.Request.Query["client_id"].ToString();

    // 4. Load client metadata
    Client? client = null;
    if (!string.IsNullOrEmpty(clientId))
    {
        client = await clientRepository.GetByClientIdAsync(clientId);
    }

    // 5. Build redirect to /authorize with parameters for client to login again
    // Use issuer from configuration if set, otherwise derive from current request.
    var issuer = builder.Configuration["TokenIssuing:Issuer"];
    var baseIssuer = !string.IsNullOrEmpty(issuer) ? issuer.TrimEnd('/') : $"{context.Request.Scheme}://{context.Request.Host}";
    var authorizeEndpoint = $"{baseIssuer}/authorize";

    if (client != null)
    {
        // Decide redirect_uri: prefer query redirect_uri if client sent it, else use client's CallbackPath or first registered redirect uri
        string? redirectUri = context.Request.Query["redirect_uri"].ToString();
        if (string.IsNullOrWhiteSpace(redirectUri))
        {
            // redirectUri ưu tiên DB
            redirectUri = client.RedirectUris?
                .Split(new[] { ' ', '\n', '\r', ';', ',' }, StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault();

            // fallback 1: từ post_logout_redirect_uri
            if (string.IsNullOrWhiteSpace(redirectUri))
                redirectUri = postLogoutRedirectUri;

            // fallback 2: bảo vệ hệ thống (tránh null)
            if (string.IsNullOrWhiteSpace(redirectUri))
                redirectUri = "/";

        }



        // Build standard authorize params
        var queryParams = new Dictionary<string, string?>();
        queryParams["client_id"] = client.ClientId;
        if (!string.IsNullOrWhiteSpace(redirectUri)) queryParams["redirect_uri"] = redirectUri;
        queryParams["response_type"] = "code";
        queryParams["scope"] = string.IsNullOrWhiteSpace(client.Scope) ? "openid profile email" : client.Scope;
        // pass-through optional PKCE / nonce / state / client info if present in original request
        var codeChallenge = context.Request.Query["code_challenge"].ToString();
        if (!string.IsNullOrEmpty(codeChallenge)) queryParams["code_challenge"] = codeChallenge;
        var codeChallengeMethod = context.Request.Query["code_challenge_method"].ToString();
        if (!string.IsNullOrEmpty(codeChallengeMethod)) queryParams["code_challenge_method"] = codeChallengeMethod;
        var nonce = context.Request.Query["nonce"].ToString();
        if (!string.IsNullOrEmpty(nonce)) queryParams["nonce"] = nonce;
        if (!string.IsNullOrEmpty(state)) queryParams["state"] = state;
        // preserve client SDK headers if client passed them as query (optional)
        var xClientSKU = context.Request.Query["x-client-SKU"].ToString();
        if (!string.IsNullOrEmpty(xClientSKU)) queryParams["x-client-SKU"] = xClientSKU;
        var xClientVer = context.Request.Query["x-client-ver"].ToString();
        if (!string.IsNullOrEmpty(xClientVer)) queryParams["x-client-ver"] = xClientVer;

        // Remove nulls and build final url
        var finalParams = queryParams
            .Where(kvp => !string.IsNullOrEmpty(kvp.Value))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value!);

        // Build back redirect URL
        var finalUrl = QueryHelpers.AddQueryString(authorizeEndpoint, finalParams);

        using var scope2 = app.Services.CreateScope();
        var logoutRepo = scope2.ServiceProvider.GetRequiredService<AuthorizationClientOne>();



        // -------------------------
        // 🔥🔥 ADD START — FRONT CHANNEL LOGOUT
        // -------------------------

        var sessions = oldSessions;   // danh sách session trước khi deactivate

         var fcUrls = sessions
            .Select(s => s.Client?.FrontChannelLogoutUri)
            .Where(url => !string.IsNullOrWhiteSpace(url))
            .Distinct()
            .ToList();

         Console.WriteLine($"[SSO LOGOUT] front-channel urls = {string.Join(" | ", fcUrls)}");

        string logoutHtml = "<html><body>";
        var sidQuery = string.IsNullOrEmpty(logoutSid) ? "" : $"?sid={logoutSid}";

        foreach (var url in fcUrls)
        {
            logoutHtml += $"<iframe src='{url}{sidQuery}' style='display:none'></iframe>";
        }

        logoutHtml += $"<script>setTimeout(()=>window.location.href='{finalUrl}',400);</script>";
        logoutHtml += "</body></html>";

        return Results.Content(logoutHtml, "text/html");





    }






    // 6. Fallback: if no client found or missing data -> try use post_logout_redirect_uri if present
    if (!string.IsNullOrEmpty(postLogoutRedirectUri))
    {
        var sep = postLogoutRedirectUri.Contains('?') ? "&" : "?";
        var redirect = string.IsNullOrEmpty(state)
            ? postLogoutRedirectUri
            : $"{postLogoutRedirectUri}{sep}state={Uri.EscapeDataString(state)}";

        return Results.Redirect(redirect);

    }

    // 7. Final fallback: OP root
    var fallback = "/";
    if (!string.IsNullOrEmpty(state))
        fallback = $"{fallback}{(fallback.Contains('?') ? "&" : "?")}state={Uri.EscapeDataString(state)}";
    return Results.Redirect(fallback);


});

// ================== HELPER FUNCTIONS (động 100%) ==================

// Xác thực client (hỗ trợ cả basic và post)
async Task<(bool IsAuthenticated, Client? Client)> AuthenticateClientAsync(
    HttpContext context, ClientOne clientRepo)
{
    string? clientId = null;
    string? clientSecret = null;

    // 1. Basic Auth
    if (context.Request.Headers.Authorization.Count > 0 &&
        context.Request.Headers.Authorization[0].StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
    {
        var header = context.Request.Headers.Authorization[0].Substring("Basic ".Length).Trim();
        var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(header));
        var parts = decoded.Split(':');
        clientId = parts[0];
        clientSecret = parts.Length > 1 ? parts[1] : null;
    }
    else
    {
        // 2. client_secret_post
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

// Validate id_token (dùng key từ JWKS của bạn)
// Sửa lại để trả về tuple có IsValid và ClaimsPrincipal — phù hợp với phần kiểm tra validationResult.IsValid trong mã gọi.
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

// Helper trả lỗi chuẩn
IResult Error(string error, string description) =>
    Results.Json(new { error, error_description = description }, statusCode: 400);





app.UseRouting();
app.UseStaticFiles();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();



// Route cho Area Admin và các Area khác
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

// Route mặc định
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

string GetCookieDomain()
{
    // ⚡ Dev vs Prod
    if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
        return "localhost"; // dev: localhost
    return ".bmwindows.vn";   // prod: main domain
}
