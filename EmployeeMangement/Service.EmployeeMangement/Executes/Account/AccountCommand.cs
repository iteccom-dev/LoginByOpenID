using DBContext.EmployeeMangement;
using DBContext.EmployeeMangement.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Authentication;
using Microsoft.EntityFrameworkCore;
using Service.EmployeeMangement.Executes.Account;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;


namespace Service.EmployeeMangement.Executes.Account
{
    public class AccountCommand
    {
        private readonly EmployeeManagementContext _employeeMangementContext;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AccountCommand(EmployeeManagementContext context, IHttpContextAccessor httpContextAccessor)
        {
            _employeeMangementContext = context;
            _httpContextAccessor = httpContextAccessor;
        }



        public async Task<bool> CheckAccount(AccountModel.AccountRequest request)
        {

            if (request == null)
            {
                return false;
            }

            var account = await _employeeMangementContext.Employees.
                FirstOrDefaultAsync(x => x.Email == request.Email && x.PasswordHash == request.PasswordHash);

            if (account == null)
            {
                return false;
            }
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, account.Id.ToString()),
                new Claim(ClaimTypes.Email, account.Email),
                new Claim("FullName", account.Fullname ?? ""),
            };

            var claimsIdentity = new ClaimsIdentity(claims, "Cookies");
            var authProperties = new Microsoft.AspNetCore.Authentication.AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTime.UtcNow.AddDays(7) // Hạn Claim
            };

            var context = _httpContextAccessor.HttpContext;
            await context.SignInAsync(
                "Cookies",
                new ClaimsPrincipal(claimsIdentity),
                authProperties
            );
            return true;
        }

        public async Task<Employee> GetAccountByEmail(string email)
        {
            return await _employeeMangementContext.Employees
                .FirstOrDefaultAsync(x => x.Email == email);
        }

        public async Task<int> Reset(string newpass, string email)
        {

            var items = await _employeeMangementContext.Employees
                .FirstOrDefaultAsync(p => p.Email == email);

            if (items == null)
                return 0;

            items.PasswordHash = newpass;

            _employeeMangementContext.Employees.Update(items);
            return await _employeeMangementContext.SaveChangesAsync();

        }

        public async Task Logout()
        {
            var context = _httpContextAccessor.HttpContext;

            // Xóa cookie đăng nhập
            await context.SignOutAsync("Cookies");
        }
    }
}