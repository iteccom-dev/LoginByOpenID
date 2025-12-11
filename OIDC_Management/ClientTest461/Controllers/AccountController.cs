using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace ClientTest461.Controllers
{
    public class AccountController : Controller
    {
        private readonly HttpClient _client;

        public AccountController()
        {
            _client = SsoHttpClient.Instance; // thay cho IHttpClientFactory
        }

        // LOGIN – giống Challenge() trong ASP.NET Core
        public ActionResult Login(string returnUrl = "/")
        {
            if (!User.Identity.IsAuthenticated)
            {
                var props = new Microsoft.Owin.Security.AuthenticationProperties
                {
                    RedirectUri = returnUrl
                };

                HttpContext.GetOwinContext()
                    .Authentication.Challenge(props, "SsoAuth");

                return new HttpUnauthorizedResult();
            }

            return Redirect(returnUrl);
        }


        // LOGOUT – chuyển toàn bộ logic từ .NET 5 sang
        public async Task<ActionResult> Logout()
        {
            var auth = HttpContext.GetOwinContext().Authentication;

            // LẤY id_token và refresh_token từ ClaimsIdentity
            var identity = User.Identity as ClaimsIdentity;
            var idToken = identity?.FindFirst("id_token")?.Value;
            var refreshToken = identity?.FindFirst("refresh_token")?.Value;

            var authority = ConfigurationManager.AppSettings["Oidc.Authority"].TrimEnd('/');

            // Thu hồi refresh_token
            if (!string.IsNullOrEmpty(refreshToken))
            {
                await RevokeTokenAsync(refreshToken, "refresh_token");
            }

            // Đăng xuất local
            auth.SignOut("Client2Auth");

            var postLogoutRedirectUri = Url.Action("LoggedOut", "Account", null, Request.Url.Scheme);

            var encodedIdToken = !string.IsNullOrEmpty(idToken)
                ? Uri.EscapeDataString(idToken)
                : string.Empty;

            // URL đăng xuất trên server SSO
            var redirectUrl =
                $"{authority}/connect/endsession" +
                $"?id_token_hint={encodedIdToken}" +
                $"&post_logout_redirect_uri={Uri.EscapeDataString(postLogoutRedirectUri)}";

            return Redirect(redirectUrl);
        }


        public ActionResult LoggedOut()
        {
            return RedirectToAction("Index", "Home");
        }


        // REVOKE TOKEN – giống .NET 5
        private async Task RevokeTokenAsync(string token, string tokenTypeHint)
        {
            var authority = ConfigurationManager.AppSettings["Oidc.Authority"].TrimEnd('/');
            var clientId = ConfigurationManager.AppSettings["Oidc.ClientId"];
            var clientSecret = ConfigurationManager.AppSettings["Oidc.ClientSecret"];

            var endpoint = $"{authority}/connect/revocation";

            var form = new Dictionary<string, string>
            {
                ["token"] = token,
                ["token_type_hint"] = tokenTypeHint,
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret
            };

            try
            {
                await _client.PostAsync(endpoint, new FormUrlEncodedContent(form));
            }
            catch
            {
                // ignore giống code .NET 5
            }
        }


        [Route("signout-callback")]
        public ActionResult SignoutCallback()
        {
            var auth = HttpContext.GetOwinContext().Authentication;
            auth.SignOut("Client2Auth");

            return Content("Client2 logged out");
        }
    }
}
