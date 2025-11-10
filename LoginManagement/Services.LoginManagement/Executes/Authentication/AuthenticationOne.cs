
using DBContexts.LoginManagement.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Services.LoginManagement.Executes.Authentication.AuthenticationModel;


namespace Services.LoginManagement.Executes.Authentication
{
    public class AuthenticationOne
    {
        private readonly LoginManagementContext _context;
        public AuthenticationOne(LoginManagementContext context)
        { 
            _context = context;
        }


        public async Task<bool> Check(AuthenRequest request)
        {
            try
            {
                return await _context.Employees
                    .AnyAsync(e => e.Email == request.Email && e.PasswordHash == request.Password);
            }
            catch
            {
                return false;
            }
        }
    }
}
