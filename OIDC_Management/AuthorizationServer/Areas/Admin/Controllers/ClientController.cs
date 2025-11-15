using Microsoft.AspNetCore.Mvc;

namespace OIDCDemo.AuthorizationServer.Areas.Admin.Controllers
{
    public class ClientController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
