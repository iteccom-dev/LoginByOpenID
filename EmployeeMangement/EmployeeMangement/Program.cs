using DBContext.EmployeeMangement;
using Microsoft.EntityFrameworkCore;
using Service.EmployeeMangement;
using Service.EmployeeMangement.Executes;
using Service.EmployeeMangement.Executes.Account;





var builder = WebApplication.CreateBuilder(args);

//builder.WebHost.ConfigureKestrel(options =>

//{

//    options.ListenAnyIP(3000); // HTTP

//    // options.ListenAnyIP(5001, listenOptions => listenOptions.UseHttps()); // HTTPS (nếu có cert)

//});



// Thêm CORS - CHO PHÉP MÁY KHÁC GỌI

builder.Services.AddCors(options =>

{

    options.AddPolicy("AllowAll", policy =>

    {

        policy.AllowAnyOrigin()

              .AllowAnyMethod()

              .AllowAnyHeader();

    });

});



// Thêm Controller

builder.Services.AddControllers();


// ------------------ Controllers + JSON ------------------
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.WriteIndented = true;
    });

// ------------------ Authentication ------------------
builder.Services.AddHttpContextAccessor();

builder.Services.AddAuthentication("Cookies")
    .AddCookie("Cookies", options =>
    {
        options.LoginPath = "/Account/SignIn";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(1);
        options.SlidingExpiration = true;
    });

// ------------------ Database ------------------
builder.Services.AddDbContext<EmployeeManagementContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ------------------ Custom Services ------------------
builder.Services.AddScoped<EmployeeMany>();
builder.Services.AddScoped<EmployeeOne>();
builder.Services.AddScoped<EmployeeCommand>();

builder.Services.AddScoped<DepartmentMany>();
builder.Services.AddScoped<DepartmentOne>();
builder.Services.AddScoped<DepartmentCommand>();

builder.Services.AddScoped<JobPositionMany>();
builder.Services.AddScoped<JobPositionOne>();
builder.Services.AddScoped<JobPositionCommand>();
builder.Services.AddScoped<AccountCommand>();
builder.Services.AddScoped<AccountModel>();


// ------------------ Build app ------------------
var app = builder.Build();

// ------------------ Middleware ------------------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
// DÙNG CORS

app.UseCors("AllowAll");
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
// ------------------ Default Route ------------------
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Employee}/{action=List}/{id?}");

app.Run();
