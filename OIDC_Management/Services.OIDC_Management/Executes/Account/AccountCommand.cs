
using DBContexts.OIDC_Management.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Authentication;
using Microsoft.EntityFrameworkCore;
using Services.OIDC_Management.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;


namespace Services.OIDC_Management.Executes
{
    public class AccountCommand
    {
        private readonly oidcIdentityContext _context;

      
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AccountCommand(oidcIdentityContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }



        public async Task<bool> CheckAccount(string email, string password)
        {

            if (email == null)
            {
                return false;
            }

            var account = await _context.AspNetUsers.FirstOrDefaultAsync(x => x.Email == email);
            if (account == null)
            {
                return false;
            }
            bool result = PasswordHelper.VerifyPassword(password, account.SecurityStamp, account.PasswordHash);
            if (result == false)
            {
                return false;
            }
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, account.Id.ToString()),
                new Claim(ClaimTypes.Email, account.Email),
                new Claim("FullName", account.UserName ?? ""),
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

        public async Task<AspNetUser> GetAccountByEmail(string email)
        {
            return await _context.AspNetUsers
                .FirstOrDefaultAsync(x => x.Email == email);
        }

       

        public async Task Logout()
        {
            var context = _httpContextAccessor.HttpContext;

            // Xóa cookie đăng nhập
            await context.SignOutAsync("Cookies");
        }
    }
}