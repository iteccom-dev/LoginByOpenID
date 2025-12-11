using Microsoft.Owin;
using Owin;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;
using System.Threading.Tasks;
using System;

[assembly: OwinStartup(typeof(test461.Startup))]

namespace test461
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            // Cookie authentication
            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = "Cookies",
                CookieName = ".client5.auth",
                CookieSecure = CookieSecureOption.Always,
            });

            // OpenId Connect
            app.UseOpenIdConnectAuthentication(new OpenIdConnectAuthenticationOptions
            {
                AuthenticationType = "oidc",
                SignInAsAuthenticationType = "Cookies",

                Authority = "https://sso-uat.iteccom.vn/",
                ClientId = "client_20251203_044718_799dd1",
                ClientSecret = "AvGSYZeq9Kj0gYaUiMctpBtsG0Wtk+eGWAttggVlVzvA=",

                RedirectUri = "https://localhost:44337/signin-oidc",
                PostLogoutRedirectUri = "https://localhost:44337/signout-callback",

                ResponseType = "code",

                Scope = "openid profile email offline_access",

                Notifications = new OpenIdConnectAuthenticationNotifications
                {
                    RedirectToIdentityProvider = n =>
                    {
                        // chạy khi logout
                        if (n.ProtocolMessage.RequestType ==
                            Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectRequestType.Logout)
                        {
                            var idToken = n.OwinContext.Authentication.User?
                                            .FindFirst("id_token")?.Value;

                            if (!string.IsNullOrEmpty(idToken))
                                n.ProtocolMessage.IdTokenHint = idToken;
                        }

                        return Task.FromResult(0);
                    }
                }
            });

            // middleware check session
            app.Use(typeof(test461.Middleware.SsoSessionValidatorMiddleware));
        }
    }
}
