using DBContext.EmployeeMangement;
using DBContext.EmployeeMangement.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Service.EmployeeMangement.Executes.EmployeeModel;

namespace Service.EmployeeMangement.Executes
{
    public class EmployeeCommand
    {
        private readonly EmployeeManagementContext _context;

        public EmployeeCommand(EmployeeManagementContext context)
        {
            _context = context;
        }

        public async Task<int> Delete(int id)
        {
            var items = await _context.Employees
                .FirstOrDefaultAsync(p => p.Id == id);

            if (items == null)
                return 0; 

            items.Status = -1; 

            _context.Employees.Update(items);
            return await _context.SaveChangesAsync();
        }
        public async Task<int> Update(EmployeeResponse request, int accountId)
        {
            if (request == null || request.Id <= 0)
                return 0;

            var items = await _context.Employees
                .FirstOrDefaultAsync(p => p.Id == request.Id);

            if (items == null)
                return 0;

            try
            {
                items.Fullname = request.Fullname;
                items.Email = request.Email;
                items.Phone = request.Phone;
                items.DepartmentId = request.DepartmentId;
                items.Position = request.Position;
                items.Status = request.Status;
                items.UpdatedBy = accountId;
                items.UpdatedDate = DateTime.UtcNow;
                items.JobPositionId = request.JobPositionId;
                items.Keyword = request.Keyword;

                return await _context.SaveChangesAsync();
            }
            catch
            {
                return -1;
            }
        }

        public async Task<int> Create(EmployeeResponse request, int accountId)
        {
            if (request == null)
                return 0;

            try
            {
                var newEmployee = new Employee
                {
                    Fullname = request.Fullname,
                    Email = request.Email,
                    Phone = request.Phone,
                    DepartmentId = request.DepartmentId,
                    Position = request.Position,
                    Status = request.Status,
                    JobPositionId = request.JobPositionId,
                    Keyword = request.Keyword,
                    CreateBy = accountId,
                    CreateDate = DateTime.UtcNow
                };

                await _context.Employees.AddAsync(newEmployee);

                return await _context.SaveChangesAsync();
            }
            catch
            {
                return -1;
            }
        }

        public async Task<int> ResetCheck(string email, int accountId)
        {

            var isChange = await _context.Employees.FirstOrDefaultAsync(p => p.Id == accountId);
            if (isChange == null)
                return 0;

            if (isChange.Role != 1)
                return 0;

            var items = await _context.Employees
                .FirstOrDefaultAsync(p => p.Email == email);
            if (items == null)
                return 0;
           
            return 1;
           
        }
        public async Task<int> Reset(string newpass, int id)
        {

            var items = await _context.Employees
                .FirstOrDefaultAsync(p => p.Id == id);

            if (items == null)
                return 0;

            items.PasswordHash = newpass;

            _context.Employees.Update(items);
            return await _context.SaveChangesAsync();

        }

        public async Task<int> Change(string newpass,  string pass, int id)
        {
            var items = await _context.Employees
                .Where(p => p.Id == id && p.PasswordHash == pass)
                .FirstOrDefaultAsync();

            if (items == null)
                return 0;

            items.PasswordHash = newpass;

            _context.Employees.Update(items);
            return await _context.SaveChangesAsync();
        }

    }
}
