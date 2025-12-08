using Microsoft.AspNetCore.Mvc;

namespace OIDCDemo.AuthorizationServer.Areas.Admin.Controllers
{
    public class SettingController : Controller
    {
        public IActionResult Setting()
        {
            return View();
        }
    }
}
