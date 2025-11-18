using Microsoft.AspNetCore.Mvc;

namespace OIDCDemo.AuthorizationServer.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View(/*"~/Areas/Admin/Views/Home/Index.cshtml"*/);
        }
        public async Task<IActionResult> ClientList()
        {
            return PartialView("Pages/Client/List");
        }
        public async Task<IActionResult> UserList()
        {
            return PartialView("Pages/User/List");
        }








    }
}
