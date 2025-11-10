using DBContext.EmployeeMangement;
using DBContext.EmployeeMangement.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Service.EmployeeMangement.Executes.EmployeeModel;

namespace Service.EmployeeMangement.Executes
{
    public class EmployeeOne
    {
        private readonly EmployeeManagementContext _context;
        public EmployeeOne(EmployeeManagementContext context)
        {
            _context = context;
        }

        public async Task<List<EmployeeResponse>> Get(int? id = 0, string? email = null)
        {
            IQueryable<Employee> query = _context.Employees
                .Include(e => e.JobPosition)
                .Include(e => e.Department)
                .Where(e => e.Status >= 0);

            if (id != null && id > 0)
            {
                query = query.Where(e => e.Id == id);
            }

            if (!string.IsNullOrWhiteSpace(email))
            {
                query = query.Where(e => e.Email == email);
            }

            var result = await query
                .Select(e => new EmployeeResponse
                {
                    Id = e.Id,
                    Keyword = e.Keyword,
                    Status = e.Status,
                    CreateBy = e.CreateBy,
                    CreateByName = _context.Employees
            .Where(u => u.Id == e.CreateBy)
            .Select(u => u.Fullname)
            .FirstOrDefault(),
                    UpdatedByName = _context.Employees
            .Where(u => u.Id == e.UpdatedBy)
            .Select(u => u.Fullname)
            .FirstOrDefault(),
                    CreateDate = e.CreateDate,
                    UpdatedBy = e.UpdatedBy,
                    UpdatedDate = e.UpdatedDate,
                    Fullname = e.Fullname,
                    Email = e.Email,
                    Phone = e.Phone,
                    Role = e.Role,
                    Position = e.Position,
                    Address = e.JobPosition != null ? e.JobPosition.Address : null,
                    JobPositionId = e.JobPosition != null ? e.JobPosition.Id : null,
                    JobPositionCode = e.JobPosition != null ? e.JobPosition.Code : null,
                    JobPositionName = e.JobPosition != null ? e.JobPosition.Name : null,
                    DepartmentName = e.Department != null ? e.Department.Name : null,
                    DepartmentId = e.Department != null ? e.Department.Id : null,
                    DepartmentCode = e.Department != null ? e.Department.Code : null
                })
                .ToListAsync();

            return result;
        }


    }
}
