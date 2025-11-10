using Microsoft.AspNetCore.Mvc;
using Services.LoginManagement.Executes.Authentication;
using Services.LoginManagement.Executes.Employee;

namespace LoginManagement.Areas.Admin.Controllers.Employee
{
    public class EmployeeController : Controller
    {
        private readonly EmployeeCommand _employeeCommand;
        public EmployeeController(EmployeeCommand employeeCommand) 
        {
            _employeeCommand = employeeCommand;
        } 
        public IActionResult Index()
        {
            return View();
        }
    }
}
