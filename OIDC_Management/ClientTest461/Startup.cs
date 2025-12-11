using Microsoft.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;
using Owin;
using System;
using System.Configuration;
using System.Security.Claims;
using System.Threading.Tasks;

[assembly: OwinStartup(typeof(ClientTest461.Startup))]
namespace ClientTest461
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            var oidcAuthority = ConfigurationManager.AppSettings["Oidc.Authority"];
            var clientId = ConfigurationManager.AppSettings["Oidc.ClientId"];
            var clientSecret = ConfigurationManager.AppSettings["Oidc.ClientSecret"];
            var callbackPath = ConfigurationManager.AppSettings["Oidc.CallbackPath"];
            var metadata = ConfigurationManager.AppSettings["Oidc.MetadataAddress"];

            // Cookie
            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = "Client2Auth",
                CookieName = ".client2.auth",
                CookieSecure = CookieSecureOption.Always,
            });

            // OIDC
            app.UseOpenIdConnectAuthentication(new OpenIdConnectAuthenticationOptions
            {
                AuthenticationType = "SsoAuth",
                Authority = oidcAuthority,
                ClientId = clientId,
                ClientSecret = clientSecret,
                RedirectUri = callbackPath,
                MetadataAddress = metadata,
                ResponseType = "code",
                SignInAsAuthenticationType = "Client2Auth",

                Notifications = new OpenIdConnectAuthenticationNotifications
                {
                    SecurityTokenValidated = ctx =>
                    {
                        var identity = (ClaimsIdentity)ctx.AuthenticationTicket.Identity;

                        var sid = identity.FindFirst("sid")?.Value;
                        if (sid != null)
                            identity.AddClaim(new Claim("sid", sid));

                        return Task.FromResult(0);
                    },

                    RedirectToIdentityProviderForSignOut = ctx =>
                    {
                        var idToken = ctx.OwinContext.Authentication.User.FindFirst("id_token")?.Value;
                        if (!string.IsNullOrEmpty(idToken))
                            ctx.ProtocolMessage.IdTokenHint = idToken;

                        return Task.FromResult(0);
                    }
                }
            });
        }
    }
}
