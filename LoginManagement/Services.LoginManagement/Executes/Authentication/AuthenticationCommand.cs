using DBContexts.LoginManagement.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.LoginManagement.Executes.Authentication
{
    public class AuthenticationCommand
    {
        private readonly LoginManagementContext _context;
        public AuthenticationCommand(LoginManagementContext context)
        { 
            _context = context;
        }
       
    }
}
