using Microsoft.IdentityModel.Tokens;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;
using Owin;
using System;
using System.Threading.Tasks;

[assembly: OwinStartup(typeof(ClientTestNF461.Startup))]

namespace ClientTestNF461
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.SetDefaultSignInAsAuthenticationType("Client2Auth");

            // 1️⃣ Cookie Authentication (PHẢI ĐĂNG KÝ ĐẦU TIÊN)
            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = "Client2Auth",
                CookieName = ".client2.auth",
                CookieSecure = CookieSecureOption.Always
            });


            // 2️⃣ OIDC Authentication
            app.UseOpenIdConnectAuthentication(new OpenIdConnectAuthenticationOptions
            {
                AuthenticationType = "SsoAuth",
                SignInAsAuthenticationType = "Client2Auth",

                Authority = "https://sso-uat.iteccom.vn/",
                ClientId = "client_20251203_044718_799dd1",
                ClientSecret = "AvGSYze9Kj0gYaLiMctp8tsG0Wtk+eGWAttgqViWzVA=",

                RedirectUri = "/signin-oidc",
                PostLogoutRedirectUri = "/signout-oidc",

                ResponseType = "code",
                Scope = "openid profile email offline_access"
            });

        }
    }
}
