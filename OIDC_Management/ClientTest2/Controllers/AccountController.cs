using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace ClientTest2.Controllers
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
                return Challenge(new AuthenticationProperties { RedirectUri = returnUrl }, "SsoAuth");

            return Redirect(returnUrl);
        }

        public async Task<IActionResult> Logout()
        {
            // Lấy id_token và refresh_token hiện tại
            var idToken = await HttpContext.GetTokenAsync("id_token");
            var refreshToken = await HttpContext.GetTokenAsync("refresh_token");

            var oidc = _configuration.GetSection("Authentication:Oidc");
            var authority = oidc["Authority"];
            if (authority == null)
                throw new Exception("Authentication:Oidc:Authority is missing!");

            authority = authority.TrimEnd('/');

            // Thu hồi refresh token nếu có
            if (!string.IsNullOrEmpty(refreshToken))
            {
                _ = RevokeTokenAsync(refreshToken, "refresh_token");
            }

            // Đăng xuất khỏi client (cookie)
            await HttpContext.SignOutAsync("Client2Auth");

            var postLogoutRedirectUri = Url.Action("LoggedOut", "Account", null, Request.Scheme);

            // Encode id_token_hint tránh lỗi URL
            var encodedIdToken = idToken != null
                ? Uri.EscapeDataString(idToken)
                : string.Empty;

            // Redirect sang SSO server để logout global
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
             }
            catch { 
            
            }
        }

        [Route("/signout-callback")]
        public async Task<IActionResult> SignoutCallback()
        {
            await HttpContext.SignOutAsync("Client2Auth");
            return Ok("Client2 logged out");
        }
    }
}
