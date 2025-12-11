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
    [Area("Admin")]
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
        // BASIC SIGN-IN
        // ================================
        [HttpGet]
        public IActionResult SignIn()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SignIn(AccountModel.AccountRequest request)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Error = "Vui lòng nhập đầy đủ thông tin đăng nhập.";
                return View(request);
            }

            var result = await _accountCommand.CheckAccount(request.Email, request.PasswordHash);
            if (!result)
            {
                ViewBag.Error = "Tên đăng nhập hoặc mật khẩu không đúng.";
                return View(request);
            }
            
            // KHÔNG SIGN IN LẠI, VÌ CLAIM ROLE ĐÃ GHI TRONG CHECKACCOUNT
            return RedirectToAction("Index", "Home");
        }

        // ================================
        // FORGOT PASSWORD
        // ================================
        public IActionResult ForgotPassword()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");
            return View();
        }

        // ================================
        // LOGIN - API VERSION
        // ================================
        [HttpGet("api/account/sign-in-view")]
        public IActionResult ApiSignInView()
        {
            return View("ApiSignIn");
        }

        [HttpPost("api/account/sign-in-view")]
        public async Task<IActionResult> ApiSignInViewPost(AccountModel.AccountRequest request)
        {
            if (!ModelState.IsValid)
                return Ok(new { success = false, message = "Thiếu thông tin đăng nhập." });

            var valid = await _accountCommand.CheckAccount(request.Email, request.PasswordHash);

            if (valid == null || !valid)
                return Ok(new { success = false, message = "Sai email hoặc mật khẩu." });

            var account = await _accountCommand.GetAccountByEmail(request.Email);

            return Ok(new
            {
                success = true,
                message = "Đăng nhập thành công",
                data = new
                {
                    Id = account.Id,
                    Email = account.Email,
                    Fullname = account.UserName
                }
            });
        }

        // ================================
        // LOGOUT
        // ================================
        public async Task<IActionResult> Logout()
        {
            await _accountCommand.Logout();
            TempData["SuccessMessage"] = "Đăng xuất thành công!";
            return RedirectToAction("SignIn");
        }


        // ================================
        // MICROSOFT ACCOUNT LOGIN (SSO Integrated)
        // ================================
        [HttpGet]
        public IActionResult LoginWithMicrosoft(string returnUrl = null)
        {
            var redirectUrl = Url.Action("LoginCallback", "Account", new { area = "Admin" });
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };

            // Lưu returnUrl vào Properties để sau khi Microsoft callback, ta biết redirect về đâu
            if (!string.IsNullOrEmpty(returnUrl))
            {
                properties.Items["returnUrl"] = returnUrl;
            }

            return Challenge(properties, MicrosoftAccountDefaults.AuthenticationScheme);
        }

        [HttpGet]
        public async Task<IActionResult> LoginCallback()
        {
            // 1. Xác thực với Microsoft
            var result = await HttpContext.AuthenticateAsync(MicrosoftAccountDefaults.AuthenticationScheme);
            if (!result.Succeeded)
                return RedirectToAction("SignIn");

            // 2. Lấy thông tin user từ Microsoft
            var userEmail = result.Principal.FindFirst(ClaimTypes.Email)?.Value
                            ?? result.Principal.Identity?.Name;
            var userName = result.Principal.FindFirst(ClaimTypes.Name)?.Value
                           ?? userEmail?.Split('@')[0] ?? "User";

            if (string.IsNullOrEmpty(userEmail))
                return RedirectToAction("SignIn");

            // 3. Tìm hoặc tạo user trong hệ thống
            var userEntity = await _authorizationClientOne.FindOrCreateUserByEmailAsync(userEmail, userName);
            if (userEntity == null)
                return RedirectToAction("SignIn");

            // 4. Lấy session time setting
            var settings = await _userOne.GetSetTime();
            var sessionTime = Convert.ToInt32(
    settings.FirstOrDefault(x => x.Section == "SetSessionTime")?.Value ?? "8"
);
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
                // Redirect về OIDC flow - hệ thống sẽ tự nhận SsoAuth cookie và cấp code
                return Redirect(returnUrl);
            }

            // Fallback: nếu không có returnUrl, redirect về trang chính
            return RedirectToAction("Index", "Home");
        }
    }
}
