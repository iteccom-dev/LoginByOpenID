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

        private void LoadUserInfo()
        {
            var claimsIdentity = User.Identity as ClaimsIdentity;

            ViewBag.Username = claimsIdentity?.FindFirst(ClaimTypes.Name)?.Value ?? "";
            ViewBag.Email = claimsIdentity?.FindFirst(ClaimTypes.Email)?.Value ?? "";
            ViewBag.Role = claimsIdentity?.FindFirst("Role")?.Value ?? "";
        }

        public IActionResult Index()
        {
            // Lấy claim Identity
            LoadUserInfo();

            // Lấy role claim
            var roleClaim = User.FindFirst("Role")?.Value;

            // Gán role vào view
            ViewBag.Role = roleClaim;

            // Debug
            ViewBag.Test = roleClaim == null
                ? "NO ROLE CLAIM FOUND"
                : $"ROLE CLAIM = {roleClaim}";

            // Nếu không phải admin → trả về trang AccessDenied
            if (roleClaim != "1")
            {
                return RedirectToAction("AccessDenied", "Home", new { area = "Admin" });
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
            LoadUserInfo();

            return View();
        }

    }
}
