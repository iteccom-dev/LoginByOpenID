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

        public IActionResult Login(string returnUrl = "/")
        {
            if (!User.Identity.IsAuthenticated)
                return Challenge(new AuthenticationProperties { RedirectUri = returnUrl }, "SsoAuth");

            return Redirect(returnUrl);
        }

        public async Task<IActionResult> Logout()
        {
            var idToken = await HttpContext.GetTokenAsync("id_token");
            var refreshToken = await HttpContext.GetTokenAsync("refresh_token");

            var oidc = _configuration.GetSection("Authentication:Oidc");
            var authority = oidc["Authority"]!.TrimEnd('/');

            if (!string.IsNullOrEmpty(refreshToken))
                _ = RevokeTokenAsync(refreshToken, "refresh_token");

            await HttpContext.SignOutAsync("Client3Auth");

            var postLogoutRedirectUri = Url.Action("LoggedOut", "Account", null, Request.Scheme);

            // 🔥 encode id_token_hint
            var encodedIdToken = Uri.EscapeDataString(idToken);

            var redirectUrl =
                $"{authority}/connect/endsession" +
                $"?id_token_hint={encodedIdToken}" +
                $"&post_logout_redirect_uri={Uri.EscapeDataString(postLogoutRedirectUri)}";

            return Redirect(redirectUrl);
        }

        public IActionResult LoggedOut()
        {
            return RedirectToAction("Index", "Home");
        }

        private async Task RevokeTokenAsync(string token, string tokenTypeHint)
        {
            var client = _httpClientFactory.CreateClient();
            var oidc = _configuration.GetSection("Authentication:Oidc");

            var endpoint = $"{oidc["Authority"]!.TrimEnd('/')}/connect/revocation";

            var formData = new Dictionary<string, string>
            {
                ["token"] = token,
                ["token_type_hint"] = tokenTypeHint,
                ["client_id"] = oidc["ClientId"]!,
                ["client_secret"] = oidc["ClientSecret"]!
            };

            try { await client.PostAsync(endpoint, new FormUrlEncodedContent(formData)); }
            catch { }
        }

        [AllowAnonymous]
        [Route("/signout-callback")]
        public async Task<IActionResult> SignoutCallback([FromQuery] string? sid)
        {
            Console.WriteLine($"[Client3] SignoutCallback called, sid = {sid}");

            // Cứ bị IdP gọi là logout local cookie
            await HttpContext.SignOutAsync("Client3Auth");

            return Ok("Client3 logged out");
        }
    }
}