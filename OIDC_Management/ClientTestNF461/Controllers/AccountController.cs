using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace ClientTestNF461.Controllers
{
    public class AccountController : Controller
    {
        private static readonly HttpClient _http = new HttpClient();

        public ActionResult Login(string returnUrl = "/")
        {
            if (!Request.IsAuthenticated)
            {
                return new ChallengeResult("SsoAuth", returnUrl);
            }

            return Redirect(returnUrl);
        }

        public async Task<ActionResult> Logout()
        {
            var auth = HttpContext.GetOwinContext().Authentication;

            var idToken = auth.User.FindFirst("id_token")?.Value;
            var refreshToken = auth.User.FindFirst("refresh_token")?.Value;

            if (!string.IsNullOrEmpty(refreshToken))
            {
                await RevokeToken(refreshToken, "refresh_token");
            }

            auth.SignOut("Client2Auth", "SsoAuth");

            var redirect = Url.Action("LoggedOut", "Account", null, Request.Url.Scheme);

            var logoutUrl =
                "https://sso-uat.iteccom.vn/connect/endsession" +
                "?id_token_hint=" + HttpUtility.UrlEncode(idToken) +
                "&post_logout_redirect_uri=" + HttpUtility.UrlEncode(redirect);

            return Redirect(logoutUrl);
        }

        private async Task RevokeToken(string token, string tokenType)
        {
            var url = "https://sso-uat.iteccom.vn/connect/revocation";

            var data = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "token", token },
                { "token_type_hint", tokenType },
                { "client_id", "client_20251203_041516_4745c2" },
                { "client_secret", "28E6DHil5Kx0F4y6vAY6EVZohHosndsUFZ+B4Zii4jY=" }
            });

            try
            {
                await _http.PostAsync(url, data);
            }
            catch { }
        }

        public ActionResult LoggedOut()
        {
            return RedirectToAction("Index", "Home");
        }
    }

    public class ChallengeResult : HttpUnauthorizedResult
    {
        private readonly string _provider;
        private readonly string _redirectUri;

        public ChallengeResult(string provider, string redirectUri)
        {
            _provider = provider;
            _redirectUri = redirectUri;
        }

        public override void ExecuteResult(ControllerContext context)
        {
            context.HttpContext.GetOwinContext().Authentication.Challenge(
                new AuthenticationProperties { RedirectUri = _redirectUri },
                _provider);
        }
    }
}
