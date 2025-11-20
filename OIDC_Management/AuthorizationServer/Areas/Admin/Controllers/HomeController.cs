using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using static Services.OIDC_Management.Executes.UserModel;

namespace OIDCDemo.AuthorizationServer.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            if (User.Identity.IsAuthenticated)
            {
                var claims = User.Identity as ClaimsIdentity;
                ViewBag.Username = claims?.FindFirst(ClaimTypes.Name)?.Value;
                ViewBag.AccountId = claims?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                ViewBag.Email = claims?.FindFirst(ClaimTypes.Email)?.Value;
                ViewBag.Name = claims?.FindFirst(ClaimTypes.Name)?.Value;
            }
            else
            {
                ViewBag.Username = "";
                ViewBag.AccountId = "";
                ViewBag.Email = "";
                ViewBag.Name = "";
            }

            return View();
        }
        public async Task<IActionResult> ClientList()
        {
            return PartialView("Pages/Client/List");
        }
        public async Task<IActionResult> UserList()
        {
            return PartialView("Pages/User/List");
        }
        public async Task<IActionResult> UserCreate()
        {
            var model = new UserResponse();
            return PartialView("Pages/User/Create", model);
        }







    }
}
