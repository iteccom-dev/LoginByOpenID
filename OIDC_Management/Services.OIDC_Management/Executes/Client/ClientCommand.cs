using DBContexts.OIDC_Management.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static Services.OIDC_Management.Executes.ClientModel;

namespace Services.OIDC_Management.Executes
{
    public class ClientCommand
    {
        private readonly oidcIdentityContext _db;

        public ClientCommand(oidcIdentityContext db)
        {
            _db = db;
        }


        //xóa
        public async Task<(bool Success, string Message)> DeleteClientById(string id)
        {
            if (string.IsNullOrEmpty(id))
                return (false, "ID không hợp lệ");

            // vì ClientId trong DB là string, so sánh thẳng
            var client = await _db.Clients.FirstOrDefaultAsync(a => a.ClientId == id);

            if (client == null)
                return (false, "Không tìm thấy client");

            var hasUsers = await _db.AspNetUsers.AnyAsync(u => u.ClientId == id);

            if (hasUsers)
                return (false, "Không thể xóa vì vẫn còn user thuộc client này.");

            client.Status = -1;
            _db.Clients.Update(client);
            await _db.SaveChangesAsync();

            return (true, "Xóa client thành công");
        }






        //thêm

        private string GenerateClientId()
        {
            var ts = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var random = Guid.NewGuid().ToString("N")[..6];
            return $"client_{ts}_{random}";
        }

        private string GenerateClientSecret()
        {
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        }

        public async Task<int> Create(ClientRequest request, int accountId)
        {
            if (request == null)
                return 0;

            try
            {
                var newClient = new Client
                {
                    ClientId = GenerateClientId(),
                    ClientSecret = GenerateClientSecret(),

                    DisplayName = request.DisplayName,

                    RedirectUris = request.RedirectUris ?? "",

                     CallbackPath = request.CallbackPath ?? "",
                      SignOutCallbackPath = request.SignOutCallbackPath ?? "",
                    AccessDeniedPath = request.AccessDeniedPath ?? "",
                    KeyWord = request.Keyword ?? "",

                    Scope = request.Scope,
                    GrantType = request.GrantType,
                    Authority = request.Authority,

                    Status = request.Status ?? 1,

                    CreatedBy = accountId,
                    CreatedDate = DateTime.UtcNow
                };

                await _db.Clients.AddAsync(newClient);
                return await _db.SaveChangesAsync();
            }
            catch
            {
                return -1;
            }
        }







        //sửa

        public async Task<int> Update(ClientRequest request, int accountId)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.ClientId))
                return 0;

            var item = await _db.Clients
                .FirstOrDefaultAsync(x => x.ClientId == request.ClientId);

            if (item == null)
                return 0;

            try
            {
                 item.DisplayName = request.DisplayName;
                item.RedirectUris = request.RedirectUris ?? "";
                item.CallbackPath = request.CallbackPath ?? "";
                item.SignOutCallbackPath = request.SignOutCallbackPath ?? "";
                item.AccessDeniedPath = request.AccessDeniedPath ?? "";
                item.Scope = request.Scope;
                item.GrantType = request.GrantType;
                item.Authority = request.Authority;
                item.KeyWord = request.Keyword ?? "";
                item.Status = request.Status ?? item.Status;

                 item.UpdatedBy = accountId;
                item.UpdatedDate = DateTime.UtcNow;

                return await _db.SaveChangesAsync();
            }
            catch
            {
                return -1;
            }
        }








    }
}
