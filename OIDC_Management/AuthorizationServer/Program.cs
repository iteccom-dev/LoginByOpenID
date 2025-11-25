
using DBContexts.OIDC_Management.Entities;
using Microsoft.AspNet.Identity;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using OIDCDemo.AuthorizationServer;
using OIDCDemo.AuthorizationServer.Helpers;
using OIDCDemo.AuthorizationServer.Models;
using Renci.SshNet.Security;
using Services.OIDC_Management.Executes;
using Services.OIDC_Management.Executes.AuthorizationClient;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<oidcIdentityContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddAuthentication("Cookies")
    .AddCookie("Cookies", options =>
    {
        options.LoginPath = "/Account/SignIn";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(1);
        options.SlidingExpiration = true;
    });

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
app.UseRouting();
app.UseStaticFiles();
app.UseAuthorization();
//// Đặt route mặc định là vào thẳng Area Admin luôn
app.MapControllerRoute(
    name: "areas",
    pattern: "{controller=Home}/{action=Index}/{id?}",
    defaults: new { area = "Admin", controller = "Home", action = "Index" });

// Route dành cho tất cả các Area (bắt buộc phải để sau route default)
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=index}/{id?}");
app.Run();
