using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace ClientTestNet8.Controllers
{
    public class HomeController : Controller
    {


        public IActionResult Index()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Redirect("/Account/Login");
            }

            ViewBag.UserName = User.Identity.Name;
            return View();
        }
        [HttpGet("signin-oidc")]
        public IActionResult CallBack()
        {
            if (User.Identity.IsAuthenticated)
            {
                return Redirect("/");
            }
            return Redirect("/Account/Login");

        }

      
    }
}
