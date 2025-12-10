using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using System.Net.Http;
using System.Threading.Tasks;

public class SsoSessionValidatorMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IHttpClientFactory _httpClientFactory;

    public SsoSessionValidatorMiddleware(RequestDelegate next, IHttpClientFactory httpClientFactory)
    {
        _next = next;
        _httpClientFactory = httpClientFactory;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var user = context.User;

        if (user == null || user.Identity == null || !user.Identity.IsAuthenticated)
        {
            await _next(context);
            return;
        }

        // Lấy SID
        var sidClaim = user.FindFirst("sid");
        var sid = sidClaim != null ? sidClaim.Value : null;

        if (sid == null)
        {
            await context.SignOutAsync("Client2Auth");
            context.Response.Redirect("/Account/Login");
            return;
        }

        // Gọi API SSO check session
        var client = _httpClientFactory.CreateClient("SsoServer");
        var resp = await client.GetAsync($"connect/check-session?sid={sid}");
        var result = await resp.Content.ReadAsStringAsync();

        if (!resp.IsSuccessStatusCode || result.Trim() != "valid")
        {
            await context.SignOutAsync("Client2Auth");
            context.Response.Redirect("/Account/Login");
            return;
        }

        await _next(context);
    }
}
