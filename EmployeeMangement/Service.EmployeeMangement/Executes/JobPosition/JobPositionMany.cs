using DBContext.EmployeeMangement;
using DBContext.EmployeeMangement.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Service.EmployeeMangement.Executes.JobPositionModel;

namespace Service.EmployeeMangement.Executes
{
    public class JobPositionMany
    {
        private readonly EmployeeManagementContext _context;
        public JobPositionMany(EmployeeManagementContext context)
        {
            _context = context;
        }
        public async Task<List<JobPosition>> JobPositions()
        {
            return await _context.JobPositions
                                 .Where(d => d.Status >=0 )
                                 .ToListAsync();
        }
        public async Task<List<JobPositionResponse>> GetAllJobPositionName()
        {
            return await _context.JobPositions
                .Where(d => d.Status >= 0)
                .Select(d => new JobPositionResponse
                {
                    Id = d.Id,
                    Code = d.Code,
                    Name = d.Name,
                    Keyword = d.Keyword,
                    Address = d.Address,
                    Status = d.Status,

                })
                .ToListAsync();
        }

    }
}
