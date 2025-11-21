using Microsoft.AspNetCore.Mvc;
using Services.OIDC_Management.Executes;
using static Services.OIDC_Management.Executes.UserModel;
using static Services.OIDC_Management.Executes.UserModel.ClientResponse;

namespace OIDCDemo.AuthorizationServer.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class UserController : Controller
    {
        private readonly UserMany _userMany;
        private readonly UserOne _userOne;
        private readonly UserCommand _userCommand;
        private readonly ClientMany _clientMany;   

        public UserController(
            UserMany userMany,
            UserCommand userCommand,
            UserOne userOne,
            ClientMany clientMany)                  
        {
            _userMany = userMany;
            _userCommand = userCommand;
            _userOne = userOne;
            _clientMany = clientMany;             
        }

        [HttpGet("api/user/gets")]
        public async Task<IActionResult> GetMany(FilterListRequest filter)
        {
            var isValid = ObjectChecker.IsValid(filter);
            if (!isValid)
            {
                return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ" });
            }

            try
            {
                var result = _userMany.GetMany(filter);
                if (result == null)
                {
                    return NotFound(new { success = false, message = "Không tìm thấy dữ liệu" });
                }
                return Ok(new
                {
                    success = true,
                    message = "Lấy dữ liệu thành công",
                    data = result
                });
            }
            catch
            {
                return StatusCode(500, new { success = false, message = "Không thể kết nối server" });
            }
        }

        [HttpGet("api/user/get/{id}")]
        public async Task<IActionResult> Get(string id)
        {
            var isValid = ObjectChecker.IsValid(id);
            if (!isValid)
            {
                return BadRequest("Dữ liệu không hợp lệ");
            }

            try
            {
                var result = await _userOne.Get(id);
                var user = result?.FirstOrDefault();

                if (user == null)
                {
                    return NotFound("Không tìm thấy dữ liệu");
                }

                 var clients = await _clientMany.GetMany();

                var model = new UserResponse
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    Status = user.Status,
                    ClientId = user.ClientId,

                     Clients = clients.Select(c => new ClientResponse
                    {
                        Id = c.Id,
                        Name = c.Name
                    }).ToList()
                };

                return PartialView("Pages/User/Create", model);
            }
            catch (Exception)
            {
                return StatusCode(500, "Không thể kết nối server");
            }
        }

        [HttpPost("api/user/create")]
        public async Task<IActionResult> Create([FromBody] UserRequest request)
        {
            var isValid = ObjectChecker.IsValid(request);
            if (!isValid)
            {
                return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ" });
            }

            try
            {
                int result = await _userCommand.Create(request);
                if (result != 2)
                {
                    return BadRequest(new { success = false, message = "Lưu không thành công" });
                }
                return Ok(new
                {
                    success = true,
                    message = "Lưu tài khoản thành công",
                });
            }
            catch
            {
                return StatusCode(500, new { success = false, message = "Không thể kết nối server" });
            }
        }

        [HttpPost("api/user/update")]
        public async Task<IActionResult> Update([FromBody] UserRequest request)
        {
            var isValid = ObjectChecker.IsValid(request);
            if (!isValid)
            {
                return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ" });
            }
            try
            {
                var result = await _userCommand.Update(request);
                if (result == 0)
                {
                    return NotFound(new { success = false, message = "Không tìm thấy dữ liệu" });
                }
                return Ok(new
                {
                    success = true,
                    message = "Cập nhật tài khoản thành công",
                });
            }
            catch
            {
                return StatusCode(500, new { success = false, message = "Không thể kết nối server" });
            }
        }

        [HttpPost("api/user/delete/{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var isValid = ObjectChecker.IsValid(id);
            if (!isValid)
            {
                return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ" });
            }

            try
            {
                var result = await _userCommand.Delete(id);
                if (result == 0)
                {
                    return NotFound(new { success = false, message = "Không tìm thấy dữ liệu" });
                }
                return Ok(new
                {
                    success = true,
                    message = "Xóa tài khoản thành công",
                });
            }
            catch
            {
                return StatusCode(500, new { success = false, message = "Không thể kết nối server" });
            }
        }
    }
}
