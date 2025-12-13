using Microsoft.IdentityModel.Protocols.OpenIdConnect;
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
            app.SetDefaultSignInAsAuthenticationType(CookieAuthenticationDefaults.AuthenticationType);

            // Cookie
            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = CookieAuthenticationDefaults.AuthenticationType,
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

                Authority = "https://sso-uat.iteccom.vn/",
                ClientId = "client_20251203_044718_799dd1",
                ClientSecret = "AvGSYze9Kj0gYaLiMctp8tsG0Wtk+eGWAttgqViWzVA=",

                RedirectUri = "https://localhost:44337/signin-oidc",
                PostLogoutRedirectUri = "https://localhost:44337/signout-callback",

                ResponseType = "code",
                Scope = "openid profile email offline_access",
                RedeemCode = true,
                UsePkce = true,

                TokenValidationParameters = new TokenValidationParameters
                {
                    NameClaimType = "name",
                    RoleClaimType = "role",
                },

                Notifications = new OpenIdConnectAuthenticationNotifications
                {
                    // 1️⃣ Lưu id_token & access_token vào Cookie
                    SecurityTokenValidated = n =>
                    {
                        var props = n.AuthenticationTicket.Properties;

                        if (n.ProtocolMessage.IdToken != null)
                        {
                            props.Dictionary["id_token"] = n.ProtocolMessage.IdToken;
                        }

                        if (n.ProtocolMessage.AccessToken != null)
                        {
                            props.Dictionary["access_token"] = n.ProtocolMessage.AccessToken;
                        }

                        if (n.ProtocolMessage.RefreshToken != null)
                        {
                            props.Dictionary["refresh_token"] = n.ProtocolMessage.RefreshToken;
                        }

                        return Task.FromResult(0);
                    },

                    // 2️⃣ Gửi id_token_hint khi logout
                    RedirectToIdentityProvider = n =>
                    {
                        if (n.ProtocolMessage.RequestType == OpenIdConnectRequestType.Logout)
                        {
                            var auth = n.OwinContext.Authentication.AuthenticateAsync("Cookies").Result;

                            if (auth?.Properties.Dictionary.ContainsKey("id_token") == true)
                            {
                                n.ProtocolMessage.IdTokenHint = auth.Properties.Dictionary["id_token"];
                            }
                        }

                        return Task.FromResult(0);
                    }
                }
            });

            app.Use(typeof(test461.Middleware.SsoSessionValidatorMiddleware));
        }
    }
}
