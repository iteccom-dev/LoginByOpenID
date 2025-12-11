using Microsoft.AspNetCore.Mvc;
using Services.OIDC_Management.Executes;

namespace OIDCDemo.AuthorizationServer.Areas.Admin.Controllers
{
    public class SettingController : Controller
    {
        private readonly UserCommand _userCommand;
        private readonly UserOne _userOne;
        private readonly IWebHostEnvironment _env;
        public IActionResult Setting()
        {
            return View();
        }
        public SettingController(UserCommand userCommand, UserOne userOne, IWebHostEnvironment env)
        {
            _userCommand = userCommand;
            _userOne = userOne;
            _env = env;
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
        [HttpGet("api/get/setlogo")]
        public async Task<IActionResult> GetSetLogo()
        {

            try
            {
                var result = await _userOne.GetPathLogo();
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
        [HttpPost("api/set-logo")]
        public async Task<IActionResult> SetLogo(IFormFile mainLogo, IFormFile smallLogo)
        {
            try
            {
                if (mainLogo == null && smallLogo == null)
                {
                    return Ok(new
                    {
                        success = false,
                        message = "Không có thay đổi để cập nhật"
                    });
                }
                // --- 1. Thư mục upload ---
                string folderPath = Path.Combine(_env.WebRootPath, "media/logo");

                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);

                string mainLogoPath = null;
                string smallLogoPath = null;

                // --- 2. Upload main logo ---
                if (mainLogo != null)
                {
                    string fileName = "logo_main_" + Guid.NewGuid() + Path.GetExtension(mainLogo.FileName);
                    string savePath = Path.Combine(folderPath, fileName);

                    using (var stream = new FileStream(savePath, FileMode.Create))
                    {
                        await mainLogo.CopyToAsync(stream);
                    }

                    mainLogoPath = "/media/logo/" + fileName;
                }

                // --- 3. Upload small logo ---
                if (smallLogo != null)
                {
                    string fileName = "logo_small_" + Guid.NewGuid() + Path.GetExtension(smallLogo.FileName);
                    string savePath = Path.Combine(folderPath, fileName);

                    using (var stream = new FileStream(savePath, FileMode.Create))
                    {
                        await smallLogo.CopyToAsync(stream);
                    }

                    smallLogoPath = "/media/logo/" + fileName;
                }

                // --- 4. Gọi hàm lưu DB ---
                var result = await _userCommand.SetLogo(mainLogoPath, smallLogoPath);

                if (result == null)
                {
                    return NotFound(new { success = false, message = "Không tìm thấy dữ liệu" });
                }

                return Ok(new
                {
                    success = true,
                    message = "Cập nhật logo thành công",
                    mainLogo = mainLogoPath,
                    smallLogo = smallLogoPath
                });
            }
            catch
            {
                return StatusCode(500, new { success = false, message = "Không thể kết nối server" });
            }
        }


    }
}
