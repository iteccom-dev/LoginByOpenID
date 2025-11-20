using Microsoft.AspNetCore.Mvc;
using Services.OIDC_Management.Executes;
using static Services.OIDC_Management.Executes.UserModel;

namespace OIDCDemo.AuthorizationServer.Areas.Admin.Controllers
{
    public class UserController : Controller
    {

        private readonly UserMany _userMany;
        public UserController(UserMany userMany) 
        { 
            _userMany = userMany;
        }
        [HttpGet("api/user/gets")]        
        public async Task<IActionResult> GetMany(FilterListRequest filter)
        {
            var isValid = ObjectChecker.IsValid(filter);
            if (isValid)
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
                return Ok( new
                {
                    success = true,
                    message = "Lấy dữ liệu thành công",
                    data = result
                });  
            }
            catch (Exception)
            {

                return StatusCode(500, new { success = false, message = "Không thể kết nối server" });

            }
          
        }





    }
}
