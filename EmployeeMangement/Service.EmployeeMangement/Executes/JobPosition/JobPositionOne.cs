using DBContext.EmployeeMangement;
using DBContext.EmployeeMangement.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.EmployeeMangement.Executes
{
    public class JobPositionOne
    {
        private readonly EmployeeManagementContext _context;
        public JobPositionOne(EmployeeManagementContext context)
        {
            _context = context;
        }

        public async Task<JobPosition?> GetJobPositionById(int id)
        {
            var jobPosition = await _context.JobPositions
                .Include(j => j.CreateByNavigation)
                .Include(j => j.UpdateByNavigation)
                .FirstOrDefaultAsync(j => j.Id == id && j.Status != -1);

            return jobPosition;
        }
    }

}
