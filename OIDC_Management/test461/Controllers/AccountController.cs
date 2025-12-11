using Microsoft.Owin.Security;
using System;
using System.Web;
using System.Web.Mvc;

namespace test461.Controllers
{
    public class AccountController : Controller
    {
        public ActionResult Login(string returnUrl = "/")
        {
            if (!Request.IsAuthenticated)
            {
                HttpContext.GetOwinContext().Authentication.Challenge(
                    new AuthenticationProperties { RedirectUri = returnUrl },
                    "oidc");
                return new EmptyResult();
            }

            return Redirect(returnUrl);
        }

        public ActionResult Logout()
        {
            var auth = HttpContext.GetOwinContext().Authentication;

            // 🔥 LẤY TOKEN TỪ AuthenticationProperties (không phải claim)
            var result = auth.AuthenticateAsync("Cookies").Result;
            string idToken = null;

            if (result?.Properties?.Dictionary != null &&
                result.Properties.Dictionary.ContainsKey("id_token"))
            {
                idToken = result.Properties.Dictionary["id_token"];
            }

            // Xóa cookie local
            auth.SignOut("Cookies");

            var authority = "https://localhost:7101";

            // Callback sau khi logout
            var postLogoutRedirectUri = Url.Action(
                "SignoutCallback",
                "Account",
                null,
                Request.Url.Scheme
            );

            // Nếu không có id_token → chỉ logout local
            if (string.IsNullOrEmpty(idToken))
            {
                return Redirect(postLogoutRedirectUri);
            }

            // 🔥 Tạo URL logout trên IdentityServer
            var logoutUrl =
                authority.TrimEnd('/') + "/connect/endsession" +
                "?id_token_hint=" + Uri.EscapeDataString(idToken) +
                "&post_logout_redirect_uri=" + Uri.EscapeDataString(postLogoutRedirectUri);

            return Redirect(logoutUrl);
        }

        public ActionResult SignoutCallback()
        {
            return RedirectToAction("Index", "Home");
        }
    }
}
