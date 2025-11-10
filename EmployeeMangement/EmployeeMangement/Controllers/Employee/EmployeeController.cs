using Azure.Core;
using EmployeeMangement.Controllers.Helper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Service.EmployeeMangement.Executes;
using Service.EmployeeMangement.Executes.Account;
using System.Security.Claims;
using static Service.EmployeeMangement.Executes.DepartmentModel;
using static Service.EmployeeMangement.Executes.EmployeeModel;
using static Service.EmployeeMangement.Executes.JobPositionModel;

namespace EmployeeMangement.Controllers
{
    [Authorize]
    public class EmployeeController : Controller
    {
        private readonly EmployeeOne _employeeOne;
        private readonly EmployeeMany _employeeMany;
        private readonly DepartmentMany _departmentMany;
        private readonly EmployeeCommand _employeeCommand;
        private readonly JobPositionMany _jobPositionMany;


        public EmployeeController(EmployeeOne employeeOne, EmployeeMany employeeMany,
            EmployeeCommand employeeCommand, DepartmentMany departmentMany, JobPositionMany jobPositionMany)
        {
            _employeeOne = employeeOne;
            _employeeMany = employeeMany;
            _employeeCommand = employeeCommand;
            _departmentMany = departmentMany;
            _jobPositionMany = jobPositionMany;
        }

        public IActionResult List()
        {
            if (User.Identity.IsAuthenticated)
            {
                var claims = User.Identity as ClaimsIdentity;
                ViewBag.Username = claims?.FindFirst(ClaimTypes.Name)?.Value;
                ViewBag.AccountId = claims?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                ViewBag.Email = claims?.FindFirst(ClaimTypes.Email)?.Value;
                ViewBag.Name = claims?.FindFirst(ClaimTypes.Name)?.Value;
            }
            else
            {
                ViewBag.Username = "";
                ViewBag.AccountId = "";
                ViewBag.Email = "";
                ViewBag.Name = "";
            }

            return View();
        }


        public IActionResult Header()
        {
            return PartialView();
        }
        public async Task<IActionResult> EmployeeList()
        {
            return PartialView("~/Views/Shared/Page/_EmployeeList.cshtml");
        }

        [HttpGet]
        public async Task<IActionResult> ChangePassword()
        {
            return PartialView("~/Views/Shared/Page/_ChangePassword.cshtml");
        }
        public async Task<IActionResult> AddEmployee()
        {
            var model = new EmployeeResponse();
            var departments = await _departmentMany.GetAllDepartmentName();

            model.Departments = departments.Select(x => new DepartmentResponse
            {
                Id = x.Id,
                Name = x.Name
            }).ToList();
            var jobpositions = await _jobPositionMany.GetAllJobPositionName();
            model.JobPositions = jobpositions.Select(y => new JobPositionResponse
            {
                Id = y.Id,
                Name = y.Name,
                Address = y.Address,

            }).ToList();
            return PartialView("~/Views/Shared/Page/_EditAddEmployee.cshtml", model);
        }

        // GET: api/employees
        [HttpGet("api/employees")]
        public async Task<IActionResult> GetAll(FilterListRequest filter)
        {

            if (filter == null) { return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ - dữ liệu rỗng" }); }

            var isValid = SqlGuard.IsSuspicious(filter);
            if (isValid) { return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ - model không hợp lệ" }); }
            try
            {
                var result = await _employeeMany.Gets(filter);
                if (result == null) { return NotFound(new { success = false, message = "Không có dữ liệu" }); }
                return Ok(new
                {
                    success = result,
                    message = "Lấy dữ liệu thành công"
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new { success = false, message = "Không thể kết nối server" });

            }
        }

        // GET: api/employees/5
        [HttpGet("api/employee/{id:int}/{mode}")]
        public async Task<IActionResult> GetById(int id = 0, string mode = "view")
        {
            if (id == 0) { return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ - dữ liệu rỗng" }); }

            var isValid = SqlGuard.IsSuspicious(id);
            if (isValid) { return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ - id không hợp lệ" }); }
            try
            {
                var result = await _employeeOne.Get(id, null);
                if (result == null || !result.Any()) { return NotFound(new { success = false, message = "Không có dữ liệu" }); }


                var employee = result.FirstOrDefault();
                if (employee == null) { return NotFound(new { success = false, message = "Không có dữ liệu" }); }

                var model = new EmployeeResponse()
                {
                    Id = employee.Id,
                    Keyword = employee.Keyword,
                    Fullname = employee.Fullname,
                    Email = employee.Email,
                    Phone = employee.Phone,
                    Position = employee.Position,
                    Status = employee.Status,
                    Role = employee.Role,
                    CreateBy = employee.CreateBy,
                    CreateByName = employee.CreateByName,
                    UpdatedByName = employee.UpdatedByName,
                    CreateDate = employee.CreateDate,
                    UpdatedBy = employee.UpdatedBy,
                    UpdatedDate = employee.UpdatedDate,
                    JobPositionName = employee.JobPositionName,
                    JobPositionId = employee.JobPositionId,
                    DepartmentName = employee.DepartmentName,
                    DepartmentId = employee.DepartmentId,
                    Address = employee.Address,
                  
                };
                if (mode == "view")
                {
                    return PartialView("~/Views/Shared/Page/_ViewDetailEmployee.cshtml", model);
                }
                var departments = await _departmentMany.GetAllDepartmentName();

                model.Departments = departments.Select(x => new DepartmentResponse
                {
                    Id = x.Id,
                    Name = x.Name
                }).ToList();
                var jobpositions = await _jobPositionMany.GetAllJobPositionName();
                model.JobPositions = jobpositions.Select(y => new JobPositionResponse
                {
                    Id = y.Id,
                    Name = y.Name,
                    Address = y.Address,

                }).ToList();
                return PartialView("~/Views/Shared/Page/_EditAddEmployee.cshtml", model);







            }
            catch (Exception)
            {
                return StatusCode(500, new { success = false, message = "Không thể kết nối server" });
            }
        }

        // GET: api/employees/5
        [HttpGet("api/employee/email/{email}")]
        public async Task<IActionResult> GetByEmail(string email)
        {
            if (email.IsNullOrEmpty()) { return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ - dữ liệu rỗng" }); }

            var isValid = SqlGuard.IsSuspicious(email);
            if (isValid) { return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ - email không hợp lệ" }); }
            try
            {
                var result = await _employeeOne.Get(0, email);
                if (result == null) { return NotFound(new { success = false, message = "Không có dữ liệu" }); }
                return Ok(new
                {
                    success = result,
                    message = "Lấy dữ liệu thành công"
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new { success = false, message = "Không thể kết nối server" });

            }
        }
        // POST: api/employees
        [HttpPost("api/employee/create")]
        public async Task<IActionResult> Create([FromBody] EmployeeResponse request)
        {
            if (request == null)
                return BadRequest(new { success = false, message = "Vui lòng điền đầy đủ thông tin" });

            if (SqlGuard.IsSuspicious(request))
                return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ" });
            try
            {
                var claims = User.Identity as ClaimsIdentity;
                var accountIdString = claims?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (!int.TryParse(accountIdString, out int accountId) || accountId <= 0)
                {
                    return Unauthorized(new { success = false, message = "Không xác thực được tài khoản" });
                }
                var result = await _employeeCommand.Create(request, accountId);
                if (result == 0)
                    return NotFound(new { success = false, message = "Không tìm thấy dữ liệu để cập nhật" });

                if (result == -1)
                    return StatusCode(500, new { success = false, message = "Lỗi khi cập nhật dữ liệu" });

                return Ok(new
                {
                    success = true,
                    message = "Lưu dữ liệu thành công"
                });
            }
            catch (Exception)
            {

                return StatusCode(500, new { success = false, message = "Không thể kết nối server" });
            }
        }


        [HttpPut("api/employee/update")]
        public async Task<IActionResult> Update([FromBody] EmployeeResponse request)
        {
            if (request == null)
                return BadRequest(new { success = false, message = "Thông tin không có thay đổi" });

            if (request.Id <= 0)
                return BadRequest(new { success = false, message = "Mã nhân viên không hợp lệ" });

            if (SqlGuard.IsSuspicious(request))
                return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ" });

            try
            {
                var claims = User.Identity as ClaimsIdentity;
                var accountIdString = claims?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (!int.TryParse(accountIdString, out int accountId) || accountId <= 0)
                {
                    return Unauthorized(new { success = false, message = "Không xác thực được tài khoản" });
                }

                var result = await _employeeCommand.Update(request, accountId);

                if (result == 0)
                    return NotFound(new { success = false, message = "Không tìm thấy dữ liệu để cập nhật" });

                if (result == -1)
                    return StatusCode(500, new { success = false, message = "Lỗi khi cập nhật dữ liệu" });

                return Ok(new
                {
                    success = true,
                    message = "Lưu dữ liệu thành công"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Không thể kết nối server: {ex.Message}" });
            }
        }


        //// DELETE: api/employee/delete/5
        [HttpPost("api/employee/delete/{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (id == 0) { return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ - dữ liệu rỗng" }); }

            var isValid = SqlGuard.IsSuspicious(id);
            if (isValid) { return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ - id không hợp lệ" }); }
            try
            {
                var result = await _employeeCommand.Delete(id);
                if (result == 0) { return NotFound(new { success = false, message = "Không có dữ liệu" }); }
                return Ok(new
                {
                    success = true,
                    message = "Xóa dữ liệu thành công"
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new { success = false, message = "Không thể kết nối server" });

            }
        }

    
        [HttpPost("api/employee/reset/{email}/{id:int}")]
        public async Task<IActionResult> Reset(string email, int id = 0)
        {
            if (id <= 0)
                return BadRequest(new { success = false, message = "Mã nhân viên không hợp lệ" });

            if (string.IsNullOrWhiteSpace(email))
                return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ - dữ liệu rỗng" });

            if (SqlGuard.IsSuspicious(email))
                return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ - email không hợp lệ" });

            var emailRegex = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            if (!System.Text.RegularExpressions.Regex.IsMatch(email, emailRegex))
                return BadRequest(new { success = false, message = "Định dạng email không hợp lệ" });

            try
            {
                var claims = User.Identity as ClaimsIdentity;
                var accountIdString = claims?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (!int.TryParse(accountIdString, out int accountId) || accountId <= 0)
                    return Unauthorized(new { success = false, message = "Không xác thực được tài khoản" });

                var checkItem = await _employeeCommand.ResetCheck(email, accountId);
                if (checkItem == 0)
                    return NotFound(new { success = false, message = "Yêu cầu không hợp lệ" });

                // ✅ Gửi email & nhận mật khẩu mới
                var newPassword = EmailHelper.SendPassword(email);
                if (newPassword == null)
                    return BadRequest(new { success = false, message = "Gửi email không thành công" });

                // ✅ Update mật khẩu mới vào DB
                var result = await _employeeCommand.Reset(newPassword, id);
                if (result == 0)
                    return StatusCode(500, new { success = false, message = "Không kết nối tới server" });

                return Ok(new
                {
                    success = true,
                    message = "Đặt lại mật khẩu thành công"
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new { success = false, message = "Không thể kết nối server" });
            }
        }

        [HttpPost("api/employee/change")]
        public async Task<IActionResult> Change(string oldpassword, string newPassword)
        {

          
            var claims = User.Identity as ClaimsIdentity;
            var accountIdString = claims?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(accountIdString, out int accountId) || accountId <= 0)
                return Unauthorized(new { success = false, message = "Không xác thực được tài khoản" });

            if (string.IsNullOrWhiteSpace(newPassword))
                return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ - dữ liệu rỗng" });

            try
            {
                var result = await _employeeCommand.Change(newPassword, oldpassword, accountId);
                if (result == 0)
                    return StatusCode(404, new { success = false, message = "Mật khẩu không chính xác" });

                return Ok(new
                {
                    success = true,
                    message = "Cập nhật mật khẩu thành công"
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new { success = false, message = "Không thể kết nối server" });
            }
        }

    }
}
