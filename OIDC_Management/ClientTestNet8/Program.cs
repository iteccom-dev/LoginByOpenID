using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

var builder = WebApplication.CreateBuilder(args);

// ------------------- Services -------------------
builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient();

// Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "Client3Auth";       // Cookie nội bộ
    options.DefaultChallengeScheme = "SsoAuth";  // OIDC login
})
.AddCookie("Client3Auth", options =>
{
    options.Cookie.Name = ".client3.auth";
})
.AddOpenIdConnect("SsoAuth", options =>
{
    var oidcConfig = builder.Configuration.GetSection("Authentication:Oidc");

    options.Authority = oidcConfig["Authority"];
    options.ClientId = oidcConfig["ClientId"];
    options.ClientSecret = oidcConfig["ClientSecret"];
    options.ResponseType = OpenIdConnectResponseType.Code;
    options.UsePkce = true;
    options.SaveTokens = true;

    options.CallbackPath = oidcConfig["CallbackPath"];
    options.SignedOutCallbackPath = oidcConfig["SignedOutCallbackPath"];
    options.MetadataAddress = oidcConfig["MetadataAddress"];

    options.SignInScheme = "Client3Auth"; // cookie nội bộ

    options.Scope.Clear();
    options.Scope.Add("openid");
    options.Scope.Add("profile");
    options.Scope.Add("email");

    options.Events = new OpenIdConnectEvents
    {
        OnAuthenticationFailed = ctx =>
        {
            Console.WriteLine("OIDC error: " + ctx.Exception.Message);
            return Task.CompletedTask;
        },
        OnTokenValidated = ctx =>
        {
            Console.WriteLine("SSO login OK: " + ctx.Principal.Identity.Name);
            return Task.CompletedTask;
        }
    };
});

var app = builder.Build();

// ------------------- Middleware -------------------
if (app.Environment.IsDevelopment())
    app.UseDeveloperExceptionPage();
else
    app.UseExceptionHandler("/Home/Error");

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// ------------------- Routes -------------------
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
