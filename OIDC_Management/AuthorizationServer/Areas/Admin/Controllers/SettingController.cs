using Microsoft.AspNetCore.Mvc;
using Services.OIDC_Management.Executes;

namespace OIDCDemo.AuthorizationServer.Areas.Admin.Controllers
{
    public class SettingController : Controller
    {
        private readonly UserCommand _userCommand;
        private readonly UserOne _userOne;
        public IActionResult Setting()
        {
            return View();
        }
        public SettingController(UserCommand userCommand, UserOne userOne)
        {
            _userCommand = userCommand;
            _userOne = userOne;
        }
        [HttpPost("api/settime")]
        public async Task<IActionResult> SetTime(int? sTime, int? rtTime)
        {
          
            try
            {
                var result = await _userCommand.SetTime(sTime,rtTime);
                if (result == false)
                {
                    return NotFound(new { success = false, message = "Không tìm thấy dữ liệu" });
                }
                return Ok(new
                {
                    success = true,
                    message = "Cập nhật thành công",
                });
            }
            catch
            {
                return StatusCode(500, new { success = false, message = "Không thể kết nối server" });
            }
        }
        [HttpGet("api/get/settime")]
        public async Task<IActionResult> GetSetTime()
        {

            try
            {
                var result = await _userOne.GetSetTime();
                if (result == null)
                {
                    return NotFound(new { success = false, message = "Không tìm thấy dữ liệu" });
                }
                return Ok(new
                {
                  success = true,
                  data = result
                });
            }
            catch
            {
                return StatusCode(500, new { success = false, message = "Không thể kết nối server" });
            }
        }

    }
}
