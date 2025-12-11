using Microsoft.IdentityModel.Tokens;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;
using Owin;
using System.Threading.Tasks;

[assembly: OwinStartup(typeof(test461.Startup))]

namespace test461
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            // 🔥 QUAN TRỌNG – thêm dòng này vào đầu tiên
            app.SetDefaultSignInAsAuthenticationType(CookieAuthenticationDefaults.AuthenticationType);

            // Cookie authentication
            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = CookieAuthenticationDefaults.AuthenticationType, // "Cookies"
                CookieName = ".client5.auth",
                CookieSecure = CookieSecureOption.Always,
                CookieSameSite = SameSiteMode.None,
                SlidingExpiration = true
            });

            // OpenId Connect
            app.UseOpenIdConnectAuthentication(new OpenIdConnectAuthenticationOptions
            {
                AuthenticationType = "oidc",
                SignInAsAuthenticationType = CookieAuthenticationDefaults.AuthenticationType,

                Authority = "https://localhost:7101/",
                ClientId = "client_20251203_044718_799dd1",
                ClientSecret = "AvGSYze9Kj0gYaLiMctp8tsG0Wtk+eGWAttgqViWzVA=",

                RedirectUri = "https://localhost:44337/signin-oidc",
                PostLogoutRedirectUri = "https://localhost:44337/signout-callback",

                ResponseType = "code",
                Scope = "openid profile email offline_access",

                SaveTokens = true,
                RedeemCode = true,
                UsePkce = true,

                TokenValidationParameters = new TokenValidationParameters
                {
                    NameClaimType = "name",
                    RoleClaimType = "role",
                },

                Notifications = new OpenIdConnectAuthenticationNotifications
                {
                    RedirectToIdentityProvider = n =>
                    {
                        if (n.ProtocolMessage.RequestType ==
                            Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectRequestType.Logout)
                        {
                            var token = n.OwinContext.Authentication.User?.FindFirst("id_token")?.Value;
                            if (!string.IsNullOrEmpty(token))
                                n.ProtocolMessage.IdTokenHint = token;
                        }
                        return Task.FromResult(0);
                    }
                }
            });

            // Middleware check session
            app.Use(typeof(test461.Middleware.SsoSessionValidatorMiddleware));
        }
    }
}
