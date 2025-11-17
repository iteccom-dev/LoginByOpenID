
using DBContexts.OIDC_Management.Entities;
using Microsoft.AspNet.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OIDCDemo.AuthorizationServer;
using OIDCDemo.AuthorizationServer.Helpers;
using OIDCDemo.AuthorizationServer.Models;
using Services.OIDC_Management.Executes;
using Services.OIDC_Management.Executes.AuthorizationClient;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<oidcIdentityContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

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
builder.Services.AddScoped<PasswordHasher>();
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
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllerRoute(
      name: "areas",
      pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}"
    );
});
app.Run();
