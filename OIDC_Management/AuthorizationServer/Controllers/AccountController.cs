using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.MicrosoftAccount;
using Microsoft.AspNetCore.Mvc;
using Services.OIDC_Management.Executes;
using Services.OIDC_Management.Executes.AuthorizationClient;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace EmployeeMangement.Controllers
{
    [Route("[controller]/[action]")]
    public class AccountController : Controller
    {
        private readonly AccountCommand _accountCommand;
        private readonly AuthorizationClientOne _authorizationClientOne;
        private readonly UserOne _userOne;

        public AccountController(
            AccountCommand accountCommand,
            AuthorizationClientOne authorizationClientOne,
            UserOne userOne)
        {
            _accountCommand = accountCommand;
            _authorizationClientOne = authorizationClientOne;
            _userOne = userOne;
        }

       
        // ================================
        // MICROSOFT ACCOUNT LOGIN (SSO Integrated)
        // ================================
        [HttpGet]
        public IActionResult LoginWithMicrosoft(string returnUrl = null)
        {
            var redirectUrl = Url.Action("LoginCallback", "Account");
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };

            // Lưu returnUrl vào Properties để sau khi Microsoft callback, ta biết redirect về đâu
            if (!string.IsNullOrEmpty(returnUrl))
            {
                properties.Items["returnUrl"] = returnUrl;
            }

            return Challenge(properties, MicrosoftAccountDefaults.AuthenticationScheme);
        }
        public string GetRedirectBaseUrl(string url)
        {
            // Parse URL (gắn domain tạm để parse)
            var uri = new Uri("https://dummy.com" + url);

            // Lấy query params
            var queryParams = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(uri.Query);

            if (!queryParams.TryGetValue("redirect_uri", out var redirectUri))
                return null;

            // decode
            string decoded = Uri.UnescapeDataString(redirectUri);

            // Parse phần redirect_uri
            var redirect = new Uri(decoded);

            // Trả về https://localhost:7097/
            return $"{redirect.Scheme}://{redirect.Host}{(redirect.IsDefaultPort ? "" : ":" + redirect.Port)}/";
        }
        [HttpGet]
        public async Task<IActionResult> LoginCallback()
        {
            try
            {
                // 1. Xác thực với Microsoft
                var result = await HttpContext.AuthenticateAsync(MicrosoftAccountDefaults.AuthenticationScheme);
                if (!result.Succeeded)
                {
                    var errorMessage = result.Failure?.Message ?? "Authentication failed";
                    return Content($"Microsoft Auth Failed: {errorMessage}");
                }

                // 2. Lấy thông tin user từ Microsoft
                var userEmail = result.Principal.FindFirst(ClaimTypes.Email)?.Value
                                ?? result.Principal.Identity?.Name;
                var userName = result.Principal.FindFirst(ClaimTypes.Name)?.Value
                               ?? userEmail?.Split('@')[0] ?? "User";

                if (string.IsNullOrEmpty(userEmail))
                    return Content("Error: Email is null or empty from Microsoft response");

                // 3. Tìm hoặc tạo user trong hệ thống
                var userEntity = await _authorizationClientOne.FindOrCreateUserByEmailAsync(userEmail, userName);
                if (userEntity == null)
                    return Content($"Error: Could not find or create user for email: {userEmail}");

                // 4. Lấy session time setting
                var settings = await _userOne.GetSetTime();
                int sessionTime = int.TryParse(settings.FirstOrDefault(x => x.Section == "SetSessionTime")?.Value, out var time) ? time : 8;

                // 5. Tạo session_state mới
                string sessionState = Guid.NewGuid().ToString("N");

                // 6. Tạo SsoAuth cookie (QUAN TRỌNG - đây là cách để SSO hoạt động!)
                await HttpContext.SignInAsync("SsoAuth", new ClaimsPrincipal(
                    new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, userEntity.Id ?? ""),
                        new Claim(ClaimTypes.Name, userEntity.UserName ?? userName),
                        new Claim(ClaimTypes.Email, userEntity.Email ?? userEmail),
                        new Claim("sid", sessionState)
                    }, "SsoAuth")),
                    new AuthenticationProperties
                    {
                        IsPersistent = true,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddHours(sessionTime)
                    });

                // 7. Lấy returnUrl từ AuthenticationProperties
                var returnUrl = result.Properties?.Items.ContainsKey("returnUrl") == true 
                    ? result.Properties.Items["returnUrl"] 
                    : null;

                if (!string.IsNullOrEmpty(returnUrl))
                {
                    var urlLog = GetRedirectBaseUrl(returnUrl);
                    // Redirect về OIDC flow - hệ thống sẽ tự nhận SsoAuth cookie và cấp code
                    return Redirect(urlLog);
                }

                // Fallback: nếu không có returnUrl, redirect về trang chính
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                // Hiển thị lỗi chi tiết để debug
                return Content($"Error in LoginCallback: {ex.Message}\n\nStackTrace: {ex.StackTrace}");
            }
        }
    }
}
