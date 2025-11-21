
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

using Services.OIDC_Management.Executes;
using Services.OIDC_Management.Executes.AuthorizationClient;
using System.Security.Claims;
using System.Threading.Tasks;

namespace EmployeeMangement.Controllers
{
    [Area("Admin")]
    public class AccountController : Controller
    {
        private readonly AccountCommand _accountCommand;

        public AccountController(AccountCommand accountCommand)
        {
            _accountCommand = accountCommand;
        }

        [HttpGet]
        public IActionResult SignIn()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
          
        }
        public IActionResult ForgotPassword()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
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
            var account = await _accountCommand.GetAccountByEmail(request.Email);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, account.Id.ToString()),
                new Claim(ClaimTypes.Email, account.Email ?? ""),
                new Claim(ClaimTypes.Name, account.UserName ?? "")
            };

            var claimsIdentity = new ClaimsIdentity(claims, "login");
            await HttpContext.SignInAsync(
                new ClaimsPrincipal(claimsIdentity),
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTime.UtcNow.AddHours(2)
                });

            return RedirectToAction("Index", "Home");
        }

        // GET: view form API
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

            
            var user = await _accountCommand.CheckAccount(request.Email, request.PasswordHash);
            if (user == null)
            {
                return Ok(new { success = false, message = "Thông tin đăng nhập không hợp lệ." });
            }
            if (!user)
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




       









        // Logout
        public async Task<IActionResult> Logout()
        {
            await _accountCommand.Logout();
            TempData["SuccessMessage"] = "Đăng xuất thành công!";
            return RedirectToAction("SignIn", "Account");
        }


        //[HttpPost("api/account/reset/{email}")]
        //public async Task<IActionResult> Reset(string email, int id = 0)
        //{
          
        //    if (string.IsNullOrWhiteSpace(email))
        //        return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ - dữ liệu rỗng" });

        //    if (SqlGuard.IsSuspicious(email))
        //        return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ - email không hợp lệ" });

        //    var emailRegex = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
        //    if (!System.Text.RegularExpressions.Regex.IsMatch(email, emailRegex))
        //        return BadRequest(new { success = false, message = "Định dạng email không hợp lệ" });

        //    try
        //    {
               
        //        var newPassword = EmailHelper.SendPassword(email);
        //        if (newPassword == null)
        //            return BadRequest(new { success = false, message = "Gửi email không thành công" });

        //        // ✅ Update mật khẩu mới vào DB
        //        var result = await _accountCommand.Reset(newPassword, email);
        //        if (result == 0)
        //            return StatusCode(500, new { success = false, message = "Không kết nối tới server" });

        //        return Ok(new
        //        {
        //            success = true,
        //            message = "Đặt lại mật khẩu thành công"
        //        });
        //    }
        //    catch (Exception)
        //    {
        //        return StatusCode(500, new { success = false, message = "Không thể kết nối server" });
        //    }
        //}

     





    }
}