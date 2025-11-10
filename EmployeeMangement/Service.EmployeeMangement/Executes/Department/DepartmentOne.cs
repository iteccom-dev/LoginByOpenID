using DBContext.EmployeeMangement;
using DBContext.EmployeeMangement.Entities;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Metadata.Ecma335;
using System.Threading.Tasks;
using static Service.EmployeeMangement.Executes.DepartmentModel;

namespace Service.EmployeeMangement
{
    public class DepartmentOne
    {
        private readonly EmployeeManagementContext _context;

        public DepartmentOne(EmployeeManagementContext context)
        {
            _context = context;
        }

        public async Task<Department?> GetDepartmentById(int id)
        {
            var department = await _context.Departments
                .Include(d => d.Manager).ThenInclude(m => m.Media)
                .Include(d => d.CreateByNavigation)
                .Include(d => d.UpdateByNavigation)
                .Include(p => p.JobPosition)
                .FirstOrDefaultAsync(d => d.Id == id && d.Status != -1);

            if (department == null)
                return null;

             if (department.Manager != null && department.Manager.Status != 1)
            {
                department.Manager = null;
                department.ManagerId = null;
            }

            return department;
        }
































        //public async Task<Department?> GetDepartmentById(int id)
        //{
        //    var department = await _context.Departments
        //        .Include(d => d.Manager)
        //            .ThenInclude(m => m.Media)  
        //        .Include(d => d.JobPosition)  
        //        .FirstOrDefaultAsync(d => d.Id == id && d.Status != -1);

        //    return department;
        //}
    }
}
