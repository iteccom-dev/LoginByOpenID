using DBContext.EmployeeMangement;
using DBContext.EmployeeMangement.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Service.EmployeeMangement.Executes.DepartmentModel;

namespace Service.EmployeeMangement.Executes
{
    public class DepartmentMany
    {
        private readonly EmployeeManagementContext _context;
        public DepartmentMany(EmployeeManagementContext context)
        {
            _context = context;
        }
        public async Task<List<Department>> GetDepartments()
        {
            return await _context.Departments
                                 .Where(d => d.Status >= 0)
                                 .ToListAsync();
        }
        public async Task<List<DepartmentResponse>> GetAllDepartmentName()
        {
            return await _context.Departments
                .Where(d => d.Status >= 0)
                .Select(d => new DepartmentResponse
                {
                    Id = d.Id,
                    Name = d.Name,
                    Status = d.Status,

                     ManagerName = d.Manager != null && d.Manager.Status == 1
                        ? d.Manager.Fullname
                        : "Chưa có",

                     EmployeeCount = d.Employees.Count(e => e.Status == 1)
                })
                .ToListAsync();
        }

    }
}
