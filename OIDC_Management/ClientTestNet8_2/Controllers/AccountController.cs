using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace ClientTestNet8.Controllers
{
    public class AccountController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public AccountController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        // ---------------- LOGIN ----------------
        public IActionResult Login(string returnUrl = "/")
        {
            if (!User.Identity.IsAuthenticated)
                return Challenge(new AuthenticationProperties { RedirectUri = returnUrl }, "SsoAuth");

            return Redirect(returnUrl);
        }

        // ---------------- LOGOUT (chuẩn OIDC) ----------------
        public async Task<IActionResult> Logout()
        {
            // ⭐ MUST GET TOKEN BEFORE SIGNOUT
            var idToken = await HttpContext.GetTokenAsync("id_token");
            var refreshToken = await HttpContext.GetTokenAsync("refresh_token");

            var oidc = _configuration.GetSection("Authentication:Oidc");
            var authority = oidc["Authority"]!.TrimEnd('/');

            // 🛑 Nếu id_token bị null → KHÔNG ĐƯỢC redirect tới /endsession
            if (string.IsNullOrEmpty(idToken))
            {
                Console.WriteLine("❌ ERROR: id_token_hint is NULL → Front-channel logout sẽ FAIL");
                // fallback logout local
                await HttpContext.SignOutAsync("Client4Auth");
                return RedirectToAction("LoggedOut");
            }

            // ⭐ REVOKE refresh_token nếu có
            if (!string.IsNullOrEmpty(refreshToken))
                _ = RevokeTokenAsync(refreshToken, "refresh_token");

            // ⭐ CLEAR LOCAL COOKIE sau khi lấy token
            await HttpContext.SignOutAsync("Client4Auth");

            // Redirect SSO trả về sau khi logout
            var postLogoutRedirectUri = Url.Action("LoggedOut", "Account", null, Request.Scheme);

            // ⭐ Encode id_token_hint để tránh lỗi 400
            var encodedIdToken = Uri.EscapeDataString(idToken);

            // ⭐ CHUẨN OIDC RP-INIT LOGOUT
            var redirectUrl =
                $"{authority}/connect/endsession" +
                $"?id_token_hint={encodedIdToken}" +
                $"&post_logout_redirect_uri={Uri.EscapeDataString(postLogoutRedirectUri)}";

            Console.WriteLine("👉 Redirecting to: " + redirectUrl);

            return Redirect(redirectUrl);
        }


        // Sau khi SSO logout xong → trả user về đây
        public IActionResult LoggedOut()
        {
            return RedirectToAction("Index", "Home");
        }

        // ---------------- REVOKE TOKEN ----------------
        private async Task RevokeTokenAsync(string token, string tokenTypeHint)
        {
            var client = _httpClientFactory.CreateClient();
            var oidcSection = _configuration.GetSection("Authentication:Oidc");

            var endpoint = $"{oidcSection["Authority"]!.TrimEnd('/')}/connect/revocation";

            var formData = new Dictionary<string, string>
            {
                ["token"] = token,
                ["token_type_hint"] = tokenTypeHint,
                ["client_id"] = oidcSection["ClientId"]!,
                ["client_secret"] = oidcSection["ClientSecret"]!
            };

            try
            {
                await client.PostAsync(endpoint, new FormUrlEncodedContent(formData));
            }
            catch
            {
                // ignore lỗi revoke — không làm fail logout
            }
        }
        [AllowAnonymous]
        [Route("/signout-callback")]
        public async Task<IActionResult> SignoutCallback([FromQuery] string? sid)
        {
            Console.WriteLine($"[Client4] SignoutCallback called, sid = {sid}");

            await HttpContext.SignOutAsync("Client4Auth");

            return Ok("Client4 logged out");
        }

    }
}