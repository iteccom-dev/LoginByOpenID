using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ClientTest2
{
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
            services.AddHttpClient();
            services.AddAuthentication(options =>
            {
                options.DefaultScheme = "Client2Auth";   // cookie nội bộ của client
                options.DefaultChallengeScheme = "SsoAuth"; // OIDC login


            })
           .AddCookie("Client2Auth", options =>
           {
               options.Cookie.Name = ".client2.auth";
               options.ExpireTimeSpan = TimeSpan.FromHours(8);
               options.Events.OnValidatePrincipal = context =>
               {
                   if (!context.Principal.Identity.IsAuthenticated)
                   {
                       // Cookie mất hiệu lực → logout
                       context.RejectPrincipal();
                   }

                   return Task.CompletedTask;
               };
           }) // cookie client
            .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
            {
                var oidcConfig = Configuration.GetSection("Authentication:Oidc");

                options.Authority = oidcConfig["Authority"];
                options.ClientId = oidcConfig["ClientId"];
                options.ClientSecret = oidcConfig["ClientSecret"];
                options.ResponseType = "code";
                options.UsePkce = true;
                options.SaveTokens = true;

                // OIDC callback
                options.CallbackPath = oidcConfig["CallbackPath"];
                options.SignedOutCallbackPath = oidcConfig["SignedOutCallbackPath"];

                // SSO discovery URL
                options.MetadataAddress = oidcConfig["MetadataAddress"];

                // ⛔ KHÔNG dùng SsoAuth
                options.SignInScheme = "Client2Auth";

                options.Scope.Clear();
                options.Scope.Add("openid");
                options.Scope.Add("profile");
                options.Scope.Add("email");

                options.Events = new OpenIdConnectEvents
                {
                    OnAuthenticationFailed = ctx =>
                    {
                        Console.WriteLine("OIDC error: " + ctx.Exception.Message);
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = ctx =>
                    {
                        Console.WriteLine("SSO login OK: " + ctx.Principal.Identity.Name);
                        return Task.CompletedTask;
                    }
                };
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();
            else
                app.UseExceptionHandler("/Home/Error");

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();
            app.UseAuthentication();  // ⚡ bắt buộc
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
