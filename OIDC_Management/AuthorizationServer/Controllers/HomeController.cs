using Microsoft.AspNetCore.Mvc;

namespace OIDCDemo.AuthorizationServer.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
