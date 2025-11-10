using EmployeeMangement.Controllers.Helper;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Client;
using Services.LoginManagement.Executes.Authentication;
using static Services.LoginManagement.Executes.Authentication.AuthenticationModel;


namespace LoginManagement.Controllers.Account
{
    public class AuthenticationController : Controller
    {
        private readonly AuthenticationOne _accountOne;

        public AuthenticationController(AuthenticationOne accountOne)
        {
            _accountOne = accountOne;
        }

        public IActionResult SignIn()
        {
            return View();
        }


        [HttpPost("api/auth/sign-in")]
        public async Task<IActionResult> SignIn(AuthenRequest request)
        {
            if (request == null) 
            {
                return BadRequest(new ApiResponse
                {
                    Success = false,
                    Message = "Không có dữ liệu nhập vào"
                });
            }
            var isVid = SqlGuard.IsSuspicious(request);
            if (isVid) 
            {
                return BadRequest(new ApiResponse
                {
                    Success = false,
                    Message = "Dữ liệu không hợp lệ"
                });
            }
            try
            {

                var result = await _accountOne.Check(request);

                if (!result)
                {
                    return NotFound(new ApiResponse
                    {
                        Success = false,
                        Message ="Không có thông tin tài khoản"
                    });
                }




                return Ok(result);



            }
            catch (Exception)
            {
                return StatusCode(500 , new ApiResponse
                {
                    Success = false,
                    Message = "Không thể kết nối server"
                });

            }






        }







    }
}
