using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text;

namespace OIDCDemo.Client.Controllers
{
    public class AuthController : Controller
    {
        [HttpGet]
        public IActionResult Login()
        {
            // Redirect to Server Login
            // Server URL: https://localhost:7101
            // Action: /Account/LoginWithMicrosoft
            return Redirect("https://localhost:7101/Account/LoginWithMicrosoft");
        }

        [HttpGet("receive-user")]
        public async Task<IActionResult> ReceiveUser(string token)
        {
            if (string.IsNullOrEmpty(token)) return BadRequest("Không có token");

            try
            {
                // 1. Decode Token
                var data = Encoding.UTF8.GetString(Convert.FromBase64String(token));
                // data format: "email=vana@...&expire=..."

                var parts = data.Split('&');
                var emailPart = parts.FirstOrDefault(p => p.StartsWith("email="));
                
                if (emailPart == null) return BadRequest("Invalid token format");

                var email = emailPart.Substring("email=".Length);
                
                // 2. Sign In
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, email),
                    new Claim(ClaimTypes.Email, email)
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTime.UtcNow.AddMinutes(30)
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                return Redirect("/");
            }
            catch (Exception ex)
            {
                return BadRequest("Error processing token: " + ex.Message);
            }
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Redirect("/");
        }
    }
}
