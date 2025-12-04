using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;
using DBContexts.OIDC_Management.Entities; // Namespace chứa class AspNetUser của bạn
using System.Text; // Để dùng Encoding
using System; // Để dùng DateTime

namespace OIDCDemo.AuthorizationServer.Controllers
{
    [AllowAnonymous]
    public class ExternalAuthController : Controller
    {
        // Sử dụng đúng class AspNetUser thay vì IdentityUser
        private readonly SignInManager<AspNetUser> _signInManager;
        private readonly UserManager<AspNetUser> _userManager;

        public ExternalAuthController(SignInManager<AspNetUser> signInManager, UserManager<AspNetUser> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }

        // ==========================================
        // 1. CỔNG VÀO (CHALLENGE)
        // ==========================================
        [HttpGet]
        public IActionResult Challenge(string provider = "Microsoft", string returnUrl = "/")
        {
            // --- CƠ CHẾ FAST TRACK (SEAMLESS SSO) ---
            // Kiểm tra: Nếu User đã đăng nhập ở Server này rồi (còn Cookie)
            // Thì không cần đẩy sang Microsoft nữa, cấp vé và trả về Client luôn.
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                // Lấy email từ Cookie hiện tại
                string email = User.FindFirst(ClaimTypes.Email)?.Value
                               ?? User.FindFirst(ClaimTypes.Name)?.Value
                               ?? User.Identity.Name;

                if (!string.IsNullOrEmpty(email))
                {
                    // Trả về Client ngay lập tức
                    return RedirectWithToken(returnUrl, email);
                }
            }

            // --- CƠ CHẾ SLOW TRACK (CHƯA ĐĂNG NHẬP) ---
            // Cấu hình để chuyển hướng sang Microsoft nhập mật khẩu
            var redirectUrl = Url.Action(nameof(Callback), "ExternalAuth", new { returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);

            // Đẩy sang Microsoft
            return Challenge(properties, provider);
        }

        // ==========================================
        // 2. XỬ LÝ KẾT QUẢ (CALLBACK)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Callback(string returnUrl = "/", string remoteError = null)
        {
            if (remoteError != null)
                return RedirectToAction("Login", "Account", new { error = $"Lỗi từ Microsoft: {remoteError}" });

            // Lấy thông tin User từ Microsoft gửi về
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
                return RedirectToAction("Login", "Account", new { error = "Không lấy được thông tin từ Microsoft." });

            // --- TRƯỜNG HỢP A: ĐÃ TỪNG LIÊN KẾT (KHÁCH QUEN) ---
            var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);

            if (result.Succeeded)
            {
                // Lấy thông tin user từ DB để tạo Token
                var userExisting = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);

                // Cấp vé và trả về Client
                return RedirectWithToken(returnUrl, userExisting.Email);
            }

            if (result.IsLockedOut)
            {
                return RedirectToAction("Lockout", "Account");
            }
            // --- TRƯỜNG HỢP B: CHƯA LIÊN KẾT (KHÁCH LẠ) ---
            else
            {
                var email = info.Principal.FindFirstValue(ClaimTypes.Email);

                if (email != null)
                {
                    // Kiểm tra xem email này đã có trong DB chưa
                    var user = await _userManager.FindByEmailAsync(email);

                    if (user == null)
                    {
                        // Nếu chưa có -> Tạo User mới (Auto Provisioning)
                        user = new AspNetUser
                        {
                            UserName = email,
                            Email = email,
                            EmailConfirmed = true,
                            Id = Guid.NewGuid().ToString(),
                            // Lưu ý: Cần đảm bảo 'Default' đã tồn tại trong bảng Clients của DB,
                            // hoặc thay bằng một ClientId có thật (ví dụ 'nhom2-client-id')
                            ClientId = "client_20251120_072451_5dc704"
                        };

                        var createResult = await _userManager.CreateAsync(user);
                        if (!createResult.Succeeded) return View("Error");
                    }

                    // Liên kết tài khoản Microsoft vào User này
                    var addLoginResult = await _userManager.AddLoginAsync(user, info);

                    if (addLoginResult.Succeeded)
                    {
                        // Đăng nhập vào Server (Tạo Cookie tại đây để lần sau dùng Fast Track)
                        await _signInManager.SignInAsync(user, isPersistent: false);

                        // Cấp vé và trả về Client
                        return RedirectWithToken(returnUrl, user.Email);
                    }
                }

                return RedirectToAction("Login", "Account", new { error = "Lỗi liên kết tài khoản." });
            }
        }

        // ==========================================
        // 3. HÀM PHỤ TRỢ: TẠO VÉ & CHUYỂN HƯỚNG
        // ==========================================
        private IActionResult RedirectWithToken(string returnUrl, string email)
        {
            // 1. Tạo nội dung vé
            var tokenData = $"email={email}&expire={DateTime.Now.AddMinutes(5).Ticks}";

            // 2. Mã hóa Base64
            var token = Convert.ToBase64String(Encoding.UTF8.GetBytes(tokenData));

            // 3. Gắn vào URL trả về
            // Kiểm tra xem returnUrl đã có dấu '?' chưa để nối chuỗi cho đúng
            string separator = returnUrl.Contains("?") ? "&" : "?";
            string finalUrl = $"{returnUrl}{separator}token={token}";

            // 4. Chuyển hướng (Dùng Redirect thường, KHÔNG dùng LocalRedirect)
            return Redirect(finalUrl);
        }
    }
}