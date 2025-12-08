using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient("SsoServer", client =>
{
    client.BaseAddress = new Uri("https://sso-uat.iteccom.vn/");
});


builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "Client3Auth";
    options.DefaultChallengeScheme = "SsoAuth";
})
.AddCookie("Client3Auth", options =>
{
    options.Cookie.Name = ".client3.auth";
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.None;

    // ⭐ MUST HAVE: lưu SID vào cookie
    options.Events.OnSigningIn = ctx =>
    {
        var sid = ctx.Principal?.FindFirst("sid");
        if (sid != null)
        {
            var identity = (ClaimsIdentity)ctx.Principal!.Identity!;
            identity.AddClaim(new Claim("sid", sid.Value));
        }
        return Task.CompletedTask;
    };
})

.AddOpenIdConnect("SsoAuth", options =>
{
    var oidc = builder.Configuration.GetSection("Authentication:Oidc");

    options.Authority = oidc["Authority"];
    options.ClientId = oidc["ClientId"];
    options.ClientSecret = oidc["ClientSecret"];


    options.ResponseType = OpenIdConnectResponseType.Code;
    options.UsePkce = true;
    options.SaveTokens = true;

    options.CallbackPath = oidc["CallbackPath"];
    options.SignedOutCallbackPath = oidc["SignedOutCallbackPath"];
    options.MetadataAddress = oidc["MetadataAddress"];

    options.SignInScheme = "Client3Auth";

    // scopes
    options.Scope.Clear();
    options.Scope.Add("openid");
    options.Scope.Add("profile");
    options.Scope.Add("email");
    options.Scope.Add("offline_access");

    // ⭐ MAP CLAIM SID
    options.ClaimActions.MapUniqueJsonKey("sid", "sid");

    // ⭐ Giữ id_token để thực hiện logout chuẩn OIDC
    options.Events = new OpenIdConnectEvents
    {
        OnRedirectToIdentityProviderForSignOut = ctx =>
        {
            var idToken = ctx.HttpContext.GetTokenAsync("id_token").Result;
            if (!string.IsNullOrEmpty(idToken))
                ctx.ProtocolMessage.IdTokenHint = idToken;

            return Task.CompletedTask;
        }
    };
});

var app = builder.Build();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseMiddleware<SsoSessionValidatorMiddleware>();
app.UseAuthorization();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();