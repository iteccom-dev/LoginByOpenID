using Microsoft.Owin;
using System;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace test461.Middleware
{
    public class SsoSessionValidatorMiddleware : OwinMiddleware
    {
        public SsoSessionValidatorMiddleware(OwinMiddleware next)
            : base(next) { }

        public override async Task Invoke(IOwinContext context)
        {
            var path = context.Request.Path.Value;

            // KHÔNG CHECK SID khi đang xử lý OIDC
            if (path == "/signin-oidc" || path == "/signout-callback")
            {
                await Next.Invoke(context);
               
            }

            var user = context.Authentication.User;

            if (user == null || !user.Identity.IsAuthenticated)
            {
                await Next.Invoke(context);
                return;
            }

            var sid = user.FindFirst("sid")?.Value;

            // ⬇️ SỬA Ở ĐÂY
            if (sid == null)
            {
                // Không có sid thì không check session, tránh loop
                await Next.Invoke(context);
                return;
            }

            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri("https://localhost:7101");
                var resp = await client.GetAsync($"connect/check-session?sid={sid}");
                var result = await resp.Content.ReadAsStringAsync();

                if (!resp.IsSuccessStatusCode || result.Trim() != "valid")
                {
                    context.Authentication.SignOut("Cookies");
                    context.Response.Redirect("/Account/Login");
                    return;
                }
            }

            await Next.Invoke(context);
        }
    }
}
