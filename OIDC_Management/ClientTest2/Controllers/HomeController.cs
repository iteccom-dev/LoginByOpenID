using Microsoft.AspNetCore.Mvc;

namespace ClientTest.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            if (User.Identity.IsAuthenticated)
                ViewBag.UserName = User.Identity.Name;
            else
                ViewBag.UserName = "Chưa login";

            return View();
        }
    }
}
