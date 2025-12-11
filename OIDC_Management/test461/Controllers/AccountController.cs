using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;
using System.Threading.Tasks;
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
            }

            return Redirect(returnUrl);
        }

        public ActionResult Logout()
        {
            HttpContext.GetOwinContext().Authentication.SignOut(
                "oidc",
                CookieAuthenticationDefaults.AuthenticationType);

            return Redirect("/");
        }

        public ActionResult LoggedOut()
        {
            return RedirectToAction("Index", "Home");
        }
    }
}
