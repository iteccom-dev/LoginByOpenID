using Microsoft.AspNetCore.Mvc;
using Service.EmployeeMangement;
using Service.EmployeeMangement.Executes;
using System.Security.Claims;
using static Service.EmployeeMangement.Executes.DepartmentModel;
using static Service.EmployeeMangement.Executes.JobPositionModel;

namespace EmployeeMangement.Controllers
{
    public class DepartmentController : Controller
    {
        private readonly DepartmentMany _departmentMany;
        private readonly DepartmentCommand _departmentCommand;
        private readonly DepartmentOne _departmentOne;

        public DepartmentController(DepartmentMany departmentMany, DepartmentCommand departmentCommand, DepartmentOne departmentOne)
        {
            _departmentMany = departmentMany;
            _departmentCommand = departmentCommand;
            _departmentOne = departmentOne;
        }
        public async Task<IActionResult> DepartmentList()
        {
            return PartialView("~/Views/Shared/Page/_DepartmentList.cshtml");
        }

        [HttpGet("api/departments/name")]
        public async Task<IActionResult> GetAllDepartmentName()
        {
            try
            {
                var results = await _departmentMany.GetAllDepartmentName();

                if (results == null || !results.Any())
                {
                    return NotFound(new { success = false, message = "Không có dữ liệu" });
                }

                return Ok(new
                {
                    success = true,
                    message = "Lấy dữ liệu thành công",
                    data = results
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new { success = false, message = "Không thể kết nối server" });
            }
        }

        [HttpPost("api/department/delete")]
        public async Task<IActionResult> DeleteDepartment([FromBody] DeleteDepartmentRequest request)
        {
            try
            {
                var result = await _departmentCommand.DeleteDepartmentById(request.Id);
                return Ok(new
                {
                    success = result.Success,
                    message = result.Message
                });

            }
            catch (Exception)
            {
                return StatusCode(500, new { success = false, message = "Lỗi kết nối server" });
            }
        }


        // === API: Lấy chi tiết phòng ban ===
        [HttpGet("api/department/view/{id}")]
        public async Task<IActionResult> GetDepartment(int id)
        {
            var result = await _departmentOne.GetDepartmentById(id);

            if (result == null)
                return BadRequest(new { success = false, message = "Không tìm thấy phòng ban" });

            return Ok(new
            {
                success = true,
                message = "Lấy chi tiết phòng ban thành công",
                data = new
                {
                    id = result.Id,
                    code = result.Code,
                    name = result.Name,
                    keyword = result.Keyword,
                    status = result.Status,
                    createBy = result.CreateByNavigation == null ? null : new
                    {
                        id = result.CreateByNavigation.Id,
                        fullName = result.CreateByNavigation.Fullname
                    },
                    createDate = result.CreateDate,
                    updateBy = result.UpdateByNavigation == null ? null : new
                    {
                        id = result.UpdateByNavigation.Id,
                        fullName = result.UpdateByNavigation.Fullname
                    },
                    updatedDate = result.UpdatedDate,
                    manager = result.Manager == null ? null : new
                    {
                        id = result.Manager.Id,
                        fullname = result.Manager.Fullname,
                        email = result.Manager.Email,
                        phone = result.Manager.Phone,
                        avatar = result.Manager.Media?.FilePath
                    },
                    jobPosition = result.JobPosition == null ? null : new
                    {
                        id = result.JobPosition.Id,
                        code = result.JobPosition.Code,
                        name = result.JobPosition.Name
                    }
                }
            });




        }

        [HttpPost("api/department/save")]
        public async Task<IActionResult> SaveDepartment([FromBody] DepartmentViewModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ" });

            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                int userId = string.IsNullOrEmpty(userIdClaim) ? 0 : int.Parse(userIdClaim);

                if (model.Id > 0)
                    model.UpdatedBy = userId;
                else
                    model.CreateBy = userId;

                var result = await _departmentCommand.SaveDepartmentAsync(model);

                string action = model.Id > 0 ? "Cập nhật" : "Thêm mới";
                return Ok(new
                {
                    success = result,
                    message = result ? $"{action} phòng ban thành công" : "Không có thay đổi dữ liệu"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi: " + ex.Message });
            }
        }



    }
}
