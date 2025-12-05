using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace ClientTest1.Controllers
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
        // login, sẽ redirect sang SSO nếu chưa có cookie
        public IActionResult Login(string returnUrl = "/")
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Challenge(new AuthenticationProperties { RedirectUri = returnUrl }, OpenIdConnectDefaults.AuthenticationScheme);
            }

            return Redirect(returnUrl);
        }

      
        public async Task<IActionResult> Logout()
        {
            // 1. Lấy token từ cookie (SaveTokens = true phải bật trong AddOpenIdConnect)
            var refreshToken = await HttpContext.GetTokenAsync("refresh_token");

            // 2. GỌI REVOKE TOKEN TRƯỚC (rất quan trọng!)
            if (!string.IsNullOrEmpty(refreshToken))
                await RevokeTokenAsync(refreshToken, "refresh_token");



            // 3. Gọi endsession + xóa cookie local
            return SignOut(
                new AuthenticationProperties
                {
                    RedirectUri = Url.Action("Index", "Home") ?? "/"
                },
                "Client1Auth",
                OpenIdConnectDefaults.AuthenticationScheme
            );
        }

        private async Task RevokeTokenAsync(string token, string tokenTypeHint)
        {
            var client = _httpClientFactory.CreateClient();

            var oidcSection = _configuration.GetSection("Authentication:Oidc");

            var clientId = oidcSection["ClientId"];
            var clientSecret = oidcSection["ClientSecret"];
            var authority = oidcSection["Authority"]?.TrimEnd('/');

            var revocationEndpoint = $"{authority}/connect/revocation";

            var formData = new Dictionary<string, string>
            {
                ["token"] = token,
                ["token_type_hint"] = tokenTypeHint,
                ["client_id"] = clientId!,
                ["client_secret"] = clientSecret!
            };

            try
            {
                var response = await client.PostAsync(revocationEndpoint, new FormUrlEncodedContent(formData));
                // Không throw nếu lỗi → không làm logout fail
            }
            catch { /* log nếu cần */ }
        }
       

        // logout, sẽ xóa cookie client, nhưng SSO cookie vẫn còn
       
    }
}
