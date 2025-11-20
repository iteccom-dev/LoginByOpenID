using DBContexts.OIDC_Management.Entities;
using Microsoft.AspNetCore.Mvc;
using NuGet.Protocol.Plugins;
using Services.OIDC_Management.Executes;
using System.Data.SqlTypes;
using System.Security.Claims;
using static Services.OIDC_Management.Executes.ClientModel;

namespace OIDCDemo.AuthorizationServer.Areas.Admin.Controllers
{
    [Area("Admin")]

     public class ClientController : Controller
    {
        private readonly ClientMany _clientMany;
        private readonly ClientOne _clientOne;
        private readonly ClientCommand _clientCommand;

        public ClientController(ClientMany clientMany, ClientOne clientOne, ClientCommand clientCommand)
        {
            _clientMany = clientMany;
            _clientOne = clientOne;
            _clientCommand = clientCommand;
        }

        [HttpGet("api/client")]
        public async Task<IActionResult> GetAll([FromQuery] ClientModel.ClientFilterRequest filter)
        {
            if (filter == null)
                return BadRequest(new { success = false, message = "Filter rỗng" });

            try
            {
                var result = await _clientMany.Gets(filter);

                return Ok(new
                {
                    success = true,
                    message = "Lấy dữ liệu thành công",
                    data = result
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new { success = false, message = "Lỗi server" });
            }
        }

        [HttpGet("api/client/{clientId?}/{mode?}")]
        public async Task<IActionResult> GetById(string clientId = "", string mode = "view")
        {
            if (string.IsNullOrWhiteSpace(clientId))
                return BadRequest(new { success = false, message = "ClientId không hợp lệ" });

            try
            {
                var clients = await _clientOne.Get(clientId);
                if (clients == null || !clients.Any())
                    return NotFound(new { success = false, message = "Không tìm thấy client" });

                var c = clients.First();

                var model = new ClientResponse()
                {
                    ClientId = c.ClientId,
                    ClientSecret = c.ClientSecret,
                    DisplayName = c.DisplayName,
                    RedirectUris = c.RedirectUris,
                    CallbackPath = c.CallbackPath,
                    AccessDeniedPath = c.AccessDeniedPath,
                    Scope = c.Scope,
                    GrantType = c.GrantType,
                    Authority = c.Authority,
                    KeyWord = c.KeyWord,
                    Status = c.Status,
                    CreatedBy = c.CreatedBy,
                    CreatedDate = c.CreatedDate,
                    UpdatedBy = c.UpdatedBy,
                    UpdatedDate = c.UpdatedDate
                };

                 if (mode == "edit-json")
                {
                    return Ok(new { success = true, data = model });
                }
 
                if (mode == "view")
                {
                    return PartialView("~/Areas/Admin/Views/Shared/Page/Detail.cshtml", model);
                }

                 ViewBag.AvailableScopes = new List<string> { "openid", "profile", "email", "api", "offline_access" };
                return PartialView("~/Areas/Admin/Views/Shared/Page/CreateUpdate.cshtml", model);
            }
            catch
            {
                return StatusCode(500, new { success = false, message = "Không thể kết nối server" });
            }
        }

        [HttpPost("api/client/delete")]
        public async Task<IActionResult> DeleteClient([FromBody] DeleteClientRequest request)
        {
            try
            {
                var result = await _clientCommand.DeleteClientById(request.Id);
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


        [HttpPost("api/client/create")]
        public async Task<IActionResult> Create([FromBody] ClientRequest request)
        {
            if (request == null)
                return BadRequest(new { success = false, message = "Vui lòng điền đầy đủ thông tin" });

            try
            {
                 int accountId = 1;

                var result = await _clientCommand.Create(request, accountId);

                if (result == 0)
                    return BadRequest(new { success = false, message = "Tạo client thất bại" });

                if (result == -1)
                    return StatusCode(500, new { success = false, message = "Lỗi khi tạo client" });

                return Ok(new
                {
                    success = true,
                    message = "Tạo client thành công"
                });
            }
            catch
            {
                return StatusCode(500, new { success = false, message = "Không thể kết nối server" });
            }
        }




        [HttpPost("api/client/update")]
        public async Task<IActionResult> Update([FromBody] ClientRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.ClientId))
                return BadRequest(new { success = false, message = "ClientId không hợp lệ" });

            if (string.IsNullOrWhiteSpace(request.DisplayName))
                return BadRequest(new { success = false, message = "Tên hiển thị không được để trống" });

            try
            {
                int accountId = 1;  

                var result = await _clientCommand.Update(request, accountId);

                if (result == 0)
                    return NotFound(new { success = false, message = "Không tìm thấy client để cập nhật" });

                if (result == -1)
                    return StatusCode(500, new { success = false, message = "Lỗi khi cập nhật dữ liệu" });

                return Ok(new
                {
                    success = true,
                    message = "Cập nhật client thành công"
                });
            }
            catch
            {
                return StatusCode(500, new { success = false, message = "Không thể kết nối server" });
            }
        }









        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> List()
        {
            return PartialView("~/Areas/Admin/Views/Client/List.cshtml");
        }

        public IActionResult CreateUpdate()
    {
        return PartialView("_CreateUpdatePartial");
    }
        public IActionResult Create()
        {
            return View();
        }

        public IActionResult Detail()
        {
            return View();
        }



        [HttpGet("api/client/export/{clientId}")]
        public async Task<IActionResult> ExportConfig(string clientId)
        {
            var clients = await _clientOne.Get(clientId);
            if (clients == null || !clients.Any())
                return NotFound(new { success = false, message = "Không tìm thấy client" });

            var c = clients.First();

            var config = new
            {
                ClientId = c.ClientId,
                ClientSecret = c.ClientSecret,
                Authority = c.Authority,
                RedirectUri = c.RedirectUris,
                Scope = c.Scope,
                GrantType = c.GrantType,
                CallbackPath = c.CallbackPath ?? "/signin-oidc",
                AccessDeniedPath = c.AccessDeniedPath ?? "/access-denied"
            };

            return Ok(new
            {
                success = true,
                data = config
            });
        }

    }
}
