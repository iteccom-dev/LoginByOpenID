using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClientTest.Controllers
{
    public class HomeController : Controller
    {
        [Authorize(AuthenticationSchemes = "Client2Auth")]
        public IActionResult Index()
        {
           
            //if (!User.Identity.IsAuthenticated)
            //{
            //    return Redirect("/Account/Login");
            //}

            ViewBag.UserName = User.Identity.Name;
            return View();
        }

    }
}
