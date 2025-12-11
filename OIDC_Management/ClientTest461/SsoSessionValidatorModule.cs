using System;
using System.Configuration;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;

namespace ClientTest461
{
    public class SsoSessionValidatorModule : IHttpModule
    {
        private static readonly HttpClient _client = new HttpClient();

        public void Init(HttpApplication context)
        {
            context.BeginRequest += async (sender, e) =>
            {
                var httpContext = HttpContext.Current;

                // Nếu chưa login → bỏ qua
                if (httpContext.User == null ||
                    httpContext.User.Identity == null ||
                    !httpContext.User.Identity.IsAuthenticated)
                {
                    return;
                }

                // Lấy claim SID
                var claimIdentity = httpContext.User.Identity as ClaimsIdentity;
                var sid = claimIdentity?.FindFirst("sid")?.Value;

                if (string.IsNullOrEmpty(sid))
                {
                    SignOutAndRedirect(httpContext);
                    return;
                }

                // URL SSO server từ Web.config
                var baseUrl = ConfigurationManager.AppSettings["SsoServerBaseUrl"];
                if (!baseUrl.EndsWith("/")) baseUrl += "/";

                var url = $"{baseUrl}connect/check-session?sid={sid}";

                try
                {
                    var resp = await _client.GetAsync(url);
                    var result = await resp.Content.ReadAsStringAsync();

                    if (!resp.IsSuccessStatusCode || result.Trim() != "valid")
                    {
                        SignOutAndRedirect(httpContext);
                        return;
                    }
                }
                catch
                {
                    // Lỗi khi gọi SSO server → cho logout luôn
                    SignOutAndRedirect(httpContext);
                }
            };
        }

        public void Dispose()
        {
        }

        private void SignOutAndRedirect(HttpContext context)
        {
            // Logout cookie auth trong OWIN
            context.GetOwinContext().Authentication.SignOut("Client2Auth");

            context.Response.Redirect("/Account/Login", endResponse: true);
        }
    }
}
