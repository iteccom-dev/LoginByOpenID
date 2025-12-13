using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

public class Startup
{
    public IConfiguration Configuration { get; }

    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllersWithViews();

        // HttpClient SSO
        services.AddHttpClient("SsoServer", client =>
        {
            client.BaseAddress = new Uri("https://localhost:7101/");
        });

        // Authentication
        services.AddAuthentication(options =>
        {
            options.DefaultScheme = "Client2Auth";
            options.DefaultChallengeScheme = "SsoAuth";
        })
        .AddCookie("Client2Auth", options =>
        {
            options.Cookie.Name = ".client2.auth";
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.None;

            // Lưu lại claim SID
            options.Events.OnSigningIn = ctx =>
            {
                var sid = ctx.Principal?.FindFirst("sid");
                if (sid != null)
                {
                    var identity = (ClaimsIdentity)ctx.Principal.Identity;
                    identity.AddClaim(new Claim("sid", sid.Value));
                }
                return Task.CompletedTask;
            };
        })
        .AddOpenIdConnect("SsoAuth", options =>
        {
            var oidc = Configuration.GetSection("Authentication:Oidc");

            options.Authority = oidc["Authority"];
            options.ClientId = oidc["ClientId"];
            options.ClientSecret = oidc["ClientSecret"];

            options.ResponseType = OpenIdConnectResponseType.Code;
            options.UsePkce = true;
            options.SaveTokens = true;

            options.CallbackPath = oidc["CallbackPath"];
            options.SignedOutCallbackPath = oidc["SignedOutCallbackPath"];
            options.MetadataAddress = oidc["MetadataAddress"];

            options.SignInScheme = "Client2Auth";

            // Scopes
            options.Scope.Clear();
            options.Scope.Add("openid");
            options.Scope.Add("profile");
            options.Scope.Add("email");
            options.Scope.Add("offline_access");

            // Map claim sid
            options.ClaimActions.MapUniqueJsonKey("sid", "sid");

            // Đính kèm id_token_hint khi logout OIDC
            options.Events = new OpenIdConnectEvents
            {
                OnRedirectToIdentityProviderForSignOut = ctx =>
                {
                    var idToken = ctx.HttpContext.GetTokenAsync("id_token").Result;
                    if (!string.IsNullOrEmpty(idToken))
                        ctx.ProtocolMessage.IdTokenHint = idToken;

                    return Task.CompletedTask;
                }
            };
        });
    }


    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();

        app.UseRouting();

        app.UseAuthentication();

        // ⭐ Middleware kiểm tra session từ SSO server
        app.UseMiddleware<SsoSessionValidatorMiddleware>();

        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");
        });
    }
}
