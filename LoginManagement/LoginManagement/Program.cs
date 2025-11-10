using Services.LoginManagement.Executes.Employee;
using DBContexts.LoginManagement.Entities;
using Microsoft.EntityFrameworkCore;
using Services.LoginManagement.Executes.Authentication;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// ------------------ Database ------------------
builder.Services.AddDbContext<LoginManagementContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ------------------ Custom Services ------------------
builder.Services.AddScoped<AuthenticationCommand>();
builder.Services.AddScoped<AuthenticationModel>();
builder.Services.AddScoped<AuthenticationOne>();
builder.Services.AddScoped<EmployeeCommand>();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllerRoute(
      name: "areas",
      pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}"
    );
});
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Authentication}/{action=SignIn}/{id?}");

app.Run();
