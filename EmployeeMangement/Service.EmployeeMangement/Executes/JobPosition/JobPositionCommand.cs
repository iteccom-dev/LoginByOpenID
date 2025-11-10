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
    public class JobPositionCommand
    {

        private readonly EmployeeManagementContext _context;

        public JobPositionCommand(EmployeeManagementContext context)
        {
            _context = context;
        }
        public async Task<(bool Success, string Message)> DeleteJobPositionById(int? id)
        {
            if (id == null)
                return (false, "ID không hợp lệ");

            var jobPosition = await _context.JobPositions.FirstOrDefaultAsync(a => a.Id == id);
            if (jobPosition == null)
                return (false, "Không tìm thấy chức vụ");

            var hasEmployees = await _context.Employees.AnyAsync(e => e.JobPositionId == id && e.Status != -1);
            if (hasEmployees)
                return (false, "Không thể xóa vì vẫn còn nhân viên đang giữ chức vụ này.");

            jobPosition.Status = -1;
            _context.JobPositions.Update(jobPosition);
            await _context.SaveChangesAsync();

            return (true, "Xóa chức vụ thành công");
        }
        public async Task<JobPosition> SaveJobPosition(JobPositionModel.JobPositionViewModel model)
        {
            JobPosition job;

            if (model.Id > 0)
            {
                job = await _context.JobPositions.FirstOrDefaultAsync(x => x.Id == model.Id);
                if (job == null)
                    throw new Exception("Không tìm thấy vị trí công tác!");

                job.Code = model.Code;
                job.Name = model.Name;
                job.Keyword = model.Keyword;
                job.Address = model.Address;
                job.Status = model.Status;
                job.UpdatedBy = model.UpdatedBy;
                job.UpdatedDate = DateTime.Now;

                _context.JobPositions.Update(job);
            }
            else
            {
                job = new JobPosition
                {
                    Code = model.Code,
                    Name = model.Name,
                    Keyword = model.Keyword,
                    Address = model.Address,
                    Status = model.Status ?? 1,
                    CreateBy = model.CreateBy,
                    CreateDate = DateTime.Now
                };
                await _context.JobPositions.AddAsync(job);
            }

            await _context.SaveChangesAsync();
            return job;
        }

    }
}

     
