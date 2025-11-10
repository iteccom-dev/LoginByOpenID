using DBContext.EmployeeMangement;
using Microsoft.AspNetCore.Mvc;
using Service.EmployeeMangement;
using Service.EmployeeMangement.Executes;
using static Service.EmployeeMangement.Executes.DepartmentModel;
using static Service.EmployeeMangement.Executes.JobPositionModel;

namespace EmployeeMangement.Controllers
{
    public class JobPositionController : Controller
    {
        private readonly JobPositionMany _jobPositionMany;
        private readonly JobPositionCommand _jobPositionCommand;
        private readonly JobPositionOne _jobPositionOne;
        public JobPositionController(JobPositionMany jobPositionMany, JobPositionCommand jobPositionCommand, JobPositionOne jobPositionOne) { 
            _jobPositionMany = jobPositionMany;
            _jobPositionCommand = jobPositionCommand;
            _jobPositionOne = jobPositionOne;
        }
        public async Task<IActionResult> JobPositionList()
        {
            return PartialView("~/Views/Shared/Page/_JobPositionList.cshtml");
        }
        [HttpGet("api/jobposition/name")]
        public async Task<IActionResult> GetAllJobPositionName()
        {
            try
            {
                var results = await _jobPositionMany.GetAllJobPositionName();

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


        [HttpPost("api/jobposition/delete")]
        public async Task<IActionResult> DeleteJobPosition([FromBody] DeleteJobPositionRequest request)
        {
            try
            {
                var result = await _jobPositionCommand.DeleteJobPositionById(request.Id);

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


        [HttpGet("api/jobposition/view/{id}")]
        public async Task<IActionResult> GetJobPosition(int id)
        {
            var result = await _jobPositionOne.GetJobPositionById(id);

            if (result == null)
                return BadRequest(new { success = false, message = "Không tìm thấy vị trí công tác" });

            return Ok(new
            {
                success = true,
                message = "Lấy chi tiết vị trí công tác thành công",
                data = new
                {
                    id = result.Id,
                    code = result.Code,
                    name = result.Name,
                    keyword = result.Keyword,
                    address = result.Address,
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
 
                }
            });
        }



        [HttpPost("api/jobposition/save")]
        public async Task<IActionResult> SaveJobPosition([FromBody] JobPositionViewModel model)
        {
            try
            {
                var result = await _jobPositionCommand.SaveJobPosition(model);
                return Ok(new
                {
                    success = true,
                    message = model.Id > 0 ? "Cập nhật vị trí công tác thành công" : "Thêm mới vị trí công tác thành công"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi server: " + ex.Message });
            }
        }

        [HttpGet("api/jobpositions/list")]
        public async Task<IActionResult> GetJobPositionList()
        {
            try
            {
                var results = await _jobPositionMany.GetAllJobPositionName();

                if (results == null || !results.Any())
                {
                    return Ok(new { success = false, message = "Không có dữ liệu chức vụ", data = new List<object>() });
                }

                // Chỉ lấy ID + Name là đủ cho dropdown
                var list = results.Select(x => new
                {
                    id = x.Id,
                    name = x.Name
                });

                return Ok(new
                {
                    success = true,
                    message = "Lấy danh sách chức vụ thành công",
                    data = list
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi server: " + ex.Message });
            }
        }


    }
}
