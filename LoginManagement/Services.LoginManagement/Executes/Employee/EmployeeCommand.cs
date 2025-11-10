using DBContexts.LoginManagement.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.LoginManagement.Executes.Employee
{
    public class EmployeeCommand
    {
        private readonly LoginManagementContext _context;
        public EmployeeCommand(LoginManagementContext context)
        { 
            _context = context;
        }
       
    }
}
