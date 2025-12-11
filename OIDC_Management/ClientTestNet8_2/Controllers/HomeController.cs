using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace ClientTestNet8.Controllers
{
    public class HomeController : Controller
    {

        [Authorize]

        public IActionResult Index()
        {
            ViewBag.UserName = User.Identity.Name;
            return View();
        }
        //[HttpGet("signin-oidc")]
        //public IActionResult CallBack()
        //{
        //    if (User.Identity.IsAuthenticated)
        //    {
        //        return Redirect("/");
        //    }
        //    return Redirect("/Account/Login");

        //}


    }
}
