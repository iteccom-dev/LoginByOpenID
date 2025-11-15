using Microsoft.AspNetCore.Mvc;

namespace OIDCDemo.AuthorizationServer.Areas.Admin.Controllers
{
    public class UserController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
