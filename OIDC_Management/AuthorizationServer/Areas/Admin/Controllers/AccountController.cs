using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.MicrosoftAccount;
using Microsoft.AspNetCore.Mvc;
using Services.OIDC_Management.Executes;
using Services.OIDC_Management.Executes.AuthorizationClient;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace EmployeeMangement.Controllers.Account
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


      
    }
}
