using System.Diagnostics;

using Microsoft.AspNetCore.Mvc;

namespace LoginManagement.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }
        public IActionResult Header()
        {
            return PartialView("~/Views/Shared/Components/_Header.cshtml");
        }
        public IActionResult Index()
        {
            return View();
        }

      



    }
}
