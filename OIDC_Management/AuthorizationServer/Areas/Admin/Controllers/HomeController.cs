using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.OIDC_Management.Executes;
using System.Security.Claims;
using static Services.OIDC_Management.Executes.UserModel;

namespace OIDCDemo.AuthorizationServer.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(AuthenticationSchemes = "Cookies")]
    public class HomeController : Controller
    {
        private readonly ClientMany _clientMany;
        public HomeController(ClientMany clientMany)
        {
            _clientMany = clientMany;
        }

        public IActionResult Index()
        {
            // Lấy claim Identity
            var claimsIdentity = User.Identity as ClaimsIdentity;

            ViewBag.Username = claimsIdentity?.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";
            ViewBag.AccountId = claimsIdentity?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            ViewBag.Email = claimsIdentity?.FindFirst(ClaimTypes.Email)?.Value;

            // Lấy role claim
            var roleClaim = claimsIdentity?.FindFirst("Role")?.Value;

            // Gán role vào view
            ViewBag.Role = roleClaim;

            // Debug
            ViewBag.Test = roleClaim == null
                ? "NO ROLE CLAIM FOUND"
                : $"ROLE CLAIM = {roleClaim}";

            // Nếu không phải admin → trả về trang AccessDenied
            if (roleClaim != "1")
            {
                return View("AccessDenied");
            }

            // Nếu là admin → load dashboard
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
            var clientsFromDb = await _clientMany.GetMany();

            var model = new UserResponse
            {
                Clients = clientsFromDb.Select(c => new ClientResponse
                {
                    Id = c.Id,
                    Name = c.Name,
                }).ToList()
            };

            return PartialView("Pages/User/Create", model);
        }

        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
