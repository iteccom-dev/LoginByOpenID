using Microsoft.AspNetCore.Authentication;

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

        if (!user.Identity?.IsAuthenticated ?? true)
        {
            await _next(context);
            return;
        }

        var sid = user.FindFirst("sid")?.Value;
        if (sid == null)
        {
            await context.SignOutAsync("Client3Auth");
            context.Response.Redirect("/Account/Login");
            return;
        }

        var client = _httpClientFactory.CreateClient("SsoServer");
        var resp = await client.GetAsync($"connect/check-session?sid={sid}");
        var result = await resp.Content.ReadAsStringAsync();

        if (!resp.IsSuccessStatusCode || result.Trim() != "valid")
        {
            await context.SignOutAsync("Client3Auth");
            context.Response.Redirect("/Account/Login");
            return;
        }

        await _next(context);
    }
}
