using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;

namespace OIDCDemo.Client.Controllers
{
    public class AccountController : Controller
    {
        // Khi user bấm nút "Đăng nhập" ở menu, gọi vào đây
        public IActionResult Login()
        {
            // Lệnh Challenge này sẽ tự động đọc config trong appsettings
            // và chuyển hướng trình duyệt sang trang Login của Nhóm 1
            return Challenge(new AuthenticationProperties
            {
                RedirectUri = "/" // Đăng nhập xong thì quay về Trang chủ
            }, 
            OpenIdConnectDefaults.AuthenticationScheme);
        }

        // Chức năng Đăng xuất
        public IActionResult Logout()
        {
            return SignOut(new AuthenticationProperties
            {
                RedirectUri = "/"
            },
            CookieAuthenticationDefaults.AuthenticationScheme,
            OpenIdConnectDefaults.AuthenticationScheme);
        }
    }
}
