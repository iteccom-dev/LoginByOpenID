using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;

namespace ClientTest.Controllers
{
    public class AccountController : Controller
    {
        // login, sẽ redirect sang SSO nếu chưa có cookie
        public IActionResult Login(string returnUrl = "/")
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Challenge(new AuthenticationProperties { RedirectUri = returnUrl }, OpenIdConnectDefaults.AuthenticationScheme);
            }

            return Redirect(returnUrl);
        }

        // logout, sẽ xóa cookie client, nhưng SSO cookie vẫn còn
        public IActionResult Logout()
        {
            return SignOut(
                new AuthenticationProperties { RedirectUri = "/" },
                CookieAuthenticationDefaults.AuthenticationScheme,
                OpenIdConnectDefaults.AuthenticationScheme
            );
        }
    }
}
