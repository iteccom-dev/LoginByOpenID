using DBContext.EmployeeMangement;
using DBContext.EmployeeMangement.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using static Service.EmployeeMangement.Executes.DepartmentModel;

namespace Service.EmployeeMangement.Executes
{
    public class DepartmentCommand
    {
        private readonly EmployeeManagementContext _context;

        public DepartmentCommand(EmployeeManagementContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Xóa 1 phòng ban (soft delete)
        /// </summary>
        public async Task<(bool Success, string Message)> DeleteDepartmentById(int? id)
        {
            if (id == null)
                return (false, "ID không hợp lệ");

            var department = await _context.Departments.FirstOrDefaultAsync(a => a.Id == id);
            if (department == null)
                return (false, "Không tìm thấy phòng ban");

            var hasEmployees = await _context.Employees.AnyAsync(e => e.DepartmentId == id && e.Status != -1);
            if (hasEmployees)
                return (false, "Không thể xóa vì vẫn còn nhân viên thuộc phòng ban này.");

            department.Status = -1;
            _context.Departments.Update(department);
            await _context.SaveChangesAsync();
            return (true, "Xóa phòng ban thành công");
        }

        /// <summary>
        /// Lưu (Thêm mới / Cập nhật) phòng ban
        /// </summary>
        public async Task<bool> SaveDepartmentAsync(DepartmentViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Name) || string.IsNullOrWhiteSpace(model.Code))
                throw new Exception("Mã phòng hoặc tên phòng không được để trống");

            Department department;
            if (model.Id > 0)
            {
                department = await _context.Departments.FirstOrDefaultAsync(d => d.Id == model.Id);
                if (department == null)
                    throw new Exception("Không tìm thấy phòng ban để cập nhật");

                department.Code = model.Code.Trim();
                department.Name = model.Name.Trim();
                department.Keyword = model.Keyword?.Trim();
                department.Status = model.Status ?? department.Status;
                department.ManagerId = model.ManagerId;
                department.JobPositionId = model.JobPositionId;
                department.UpdatedBy = model.UpdatedBy;
                department.UpdatedDate = DateTime.Now;

                _context.Departments.Update(department);

                var updatePos = await _context.Employees
                     .Where(p => p.Id == model.ManagerId)
                     .FirstOrDefaultAsync();

                updatePos.Position = "Manager";

                _context.Update(updatePos);

            }
            else
            {
                department = new Department
                {
                    Code = model.Code.Trim(),
                    Name = model.Name.Trim(),
                    Keyword = model.Keyword?.Trim(),
                    Status = model.Status ?? 1,
                    ManagerId = model.ManagerId,
                    JobPositionId = model.JobPositionId,
                    CreateBy = model.CreateBy,
                    CreateDate = DateTime.Now
                };

                await _context.Departments.AddAsync(department);
            }

            return await _context.SaveChangesAsync() > 0;
        }
    }
}
