using Microsoft.AspNetCore.Mvc;

namespace OIDCDemo.AuthorizationServer.Areas.Admin.Controllers
{
    [Area("Admin")]

    public class ClientController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }


        public async Task<IActionResult> List()
        {
            return PartialView("~/Areas/Admin/Views/Client/List.cshtml");
        }

        public IActionResult Create()
        {
            return View();
        }

        public IActionResult Detail()
        {
            return View();
        }
    }
}
