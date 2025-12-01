using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;
using DBContexts.OIDC_Management.Entities; // Namespace chứa AspNetUser
using System.Text; // Để dùng Encoding
using System; // Để dùng DateTime

namespace OIDCDemo.AuthorizationServer.Controllers
{
    [AllowAnonymous]
    public class ExternalAuthController : Controller
    {
        private readonly SignInManager<AspNetUser> _signInManager;
        private readonly UserManager<AspNetUser> _userManager;

        public ExternalAuthController(SignInManager<AspNetUser> signInManager, UserManager<AspNetUser> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }

        [HttpGet]
        public IActionResult Challenge(string provider = "Microsoft", string returnUrl = "/")
        {
            // --- KIỂM TRA ĐĂNG NHẬP NHANH (Fast Track) ---
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                // Lấy email từ User đang đăng nhập
                string email = User.FindFirst(ClaimTypes.Email)?.Value
                               ?? User.FindFirst(ClaimTypes.Name)?.Value
                               ?? User.Identity.Name;

                if (!string.IsNullOrEmpty(email))
                {
                    return RedirectWithToken(returnUrl, email);
                }
            }

            // --- NẾU CHƯA ĐĂNG NHẬP -> GỌI MICROSOFT ---
            var redirectUrl = Url.Action(nameof(Callback), "ExternalAuth", new { returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return Challenge(properties, provider);
        }

        [HttpGet]
        public async Task<IActionResult> Callback(string returnUrl = "/", string remoteError = null)
        {
            if (remoteError != null)
                return RedirectToAction("Login", "Account", new { error = $"Lỗi từ Microsoft: {remoteError}" });

            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
                return RedirectToAction("Login", "Account", new { error = "Không lấy được thông tin." });

            // --- TRƯỜNG HỢP 1: ĐÃ TỪNG LIÊN KẾT TÀI KHOẢN ---
            var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);

            if (result.Succeeded)
            {
                // Lấy thông tin user để tạo Token
                var userExisting = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
                return RedirectWithToken(returnUrl, userExisting.Email);
            }

            if (result.IsLockedOut)
            {
                return RedirectToAction("Lockout", "Account");
            }
            else
            {
                // --- TRƯỜNG HỢP 2: CHƯA LIÊN KẾT (USER MỚI HOẶC CŨ) ---
                var email = info.Principal.FindFirstValue(ClaimTypes.Email);

                if (email != null)
                {
                    // Tìm xem email này đã có trong DB chưa
                    var user = await _userManager.FindByEmailAsync(email); // <--- BIẾN 'user' KHAI BÁO TẠI ĐÂY

                    if (user == null)
                    {
                        // Nếu chưa có -> Tạo User mới
                        user = new AspNetUser
                        {
                            UserName = email,
                            Email = email,
                            EmailConfirmed = true,
                            Id = Guid.NewGuid().ToString(),
                            ClientId = "Default" // Đảm bảo ClientId này hợp lệ trong DB
                        };
                        var createResult = await _userManager.CreateAsync(user);
                        if (!createResult.Succeeded) return View("Error");
                    }

                    // Liên kết tài khoản Microsoft vào User này
                    var addLoginResult = await _userManager.AddLoginAsync(user, info); // <--- BIẾN 'addLoginResult' KHAI BÁO TẠI ĐÂY

                    if (addLoginResult.Succeeded)
                    {
                        // Đăng nhập vào Cookie Server
                        await _signInManager.SignInAsync(user, isPersistent: false);

                        // Tạo Token và trả về Client
                        return RedirectWithToken(returnUrl, user.Email);
                    }
                }

                return RedirectToAction("Login", "Account", new { error = "Lỗi liên kết tài khoản." });
            }
        }

        // --- HÀM PHỤ TRỢ: GẮN TOKEN VÀO URL ---
        private IActionResult RedirectWithToken(string returnUrl, string email)
        {
            var tokenData = $"email={email}&expire={DateTime.Now.AddMinutes(5).Ticks}";
            var token = Convert.ToBase64String(Encoding.UTF8.GetBytes(tokenData));

            string separator = returnUrl.Contains("?") ? "&" : "?";
            string finalUrl = $"{returnUrl}{separator}token={token}";

            return Redirect(finalUrl);
        }
    }
}