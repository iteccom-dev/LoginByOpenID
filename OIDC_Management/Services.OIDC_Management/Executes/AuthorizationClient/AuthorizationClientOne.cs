using DBContexts.OIDC_Management.Entities;
using Microsoft.EntityFrameworkCore;
using Services.OIDC_Management.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Services.OIDC_Management.Executes.AuthorizationClient.AuthorizationClientModel;

namespace Services.OIDC_Management.Executes.AuthorizationClient
{
    public class AuthorizationClientOne
    {
        private readonly oidcIdentityContext _db;

        public AuthorizationClientOne(oidcIdentityContext db)
        {
            _db = db;
        }

        public async Task<Client?> FindByClientId(string clientId)
        {
            var resutl = await _db.Clients.FirstOrDefaultAsync(c => c.ClientId == clientId);
            return resutl;
        }
       
        public async Task<AuthUserInfo?> CheckAccount(string email, string password, string clientId)
        {
            // Lấy user theo email
            var userEntity = await _db.AspNetUsers.FirstOrDefaultAsync(x => x.Email == email);
            if (userEntity == null)
                return null;

            // Kiểm tra user có thuộc clientId hay không
            bool isOfClient = await _db.AspNetUsers
                .AnyAsync(u => u.Email == email && u.ClientId == clientId);

            if (!isOfClient)
                return null;

            // Kiểm tra mật khẩu
            bool result = PasswordHelper.VerifyPassword(password, userEntity.SecurityStamp, userEntity.PasswordHash);
            if (!result)
                return null;

            // Trả về AuthUserInfo
            return new AuthUserInfo
            {
                UserId = userEntity.Id.ToString(), // nếu Id là Guid
                Username = userEntity.UserName,    // hoặc userEntity.Username tùy DB
                Email = userEntity.Email
            };
        }



    }
}
