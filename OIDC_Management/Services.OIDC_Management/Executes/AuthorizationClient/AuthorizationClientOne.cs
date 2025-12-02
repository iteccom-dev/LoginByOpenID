using DBContexts.OIDC_Management.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq;
using Services.OIDC_Management.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Services.OIDC_Management.Executes.AuthorizationClient.AuthorizationClientModel;
using RefreshToken = DBContexts.OIDC_Management.Entities.RefreshToken;

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
        public async Task<AspNetUser?> FindByUserId(string userId)
        {
            var resutl = await _db.AspNetUsers.FirstOrDefaultAsync(c => c.Id == userId);
            return resutl;
        }
        public async Task<AuthUserInfo?> CheckAccount(string email, string password)
        {
            // Lấy user theo email
            var userEntity = await _db.AspNetUsers.FirstOrDefaultAsync(x => x.Email == email);
            if (userEntity == null)
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


        public async Task<RefreshToken?> CreateOrReplaceRefreshTokenAsync(
       string userId,
       string clientId,
       string scope,
       string? oldRefreshToken = null,
       TimeSpan? validity = null)
        {
            validity ??= TimeSpan.FromHours(8); // mặc định 8 giờ   

            using var transaction = await _db.Database.BeginTransactionAsync();

            RefreshToken? oldTokenEntity = null;

            // 1. Nếu có truyền oldRefreshToken → kiểm tra xem có hợp lệ không
            if (!string.IsNullOrWhiteSpace(oldRefreshToken))
            {
                oldTokenEntity = await _db.RefreshTokens
                    .FirstOrDefaultAsync(t =>
                        t.Token == oldRefreshToken &&
                        t.UserId == userId &&
                        t.ClientId == clientId &&
                        t.ExpiresTime > DateTime.UtcNow);

                // Nếu token hợp lệ → cập nhật lại token đó
                if (oldTokenEntity != null)
                {
                    oldTokenEntity.Token = GenerateRefreshToken();
                    oldTokenEntity.Scope = scope;
                    oldTokenEntity.CreatedTime = DateTime.UtcNow;
                    oldTokenEntity.ExpiresTime = DateTime.UtcNow.Add(validity.Value);

                    _db.RefreshTokens.Update(oldTokenEntity);
                    await _db.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return oldTokenEntity;
                }
                return null;
            }
            var rowsAffected = await _db.RefreshTokens
               .Where(t => t.UserId == userId && t.ClientId == clientId)
               .ExecuteDeleteAsync();
            // 2. Nếu không có hoặc không hợp lệ → tạo token mới
            var newToken = new RefreshToken
            {
                Token = GenerateRefreshToken(),
                UserId = userId,
                ClientId = clientId,
                Scope = scope,
                CreatedTime = DateTime.UtcNow,
                ExpiresTime = DateTime.UtcNow.Add(validity.Value),
            };

            _db.RefreshTokens.Add(newToken);

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            return newToken;
        }

        public async Task<bool> RevokeTokenAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return false;

            // EF Core 7/8/9 (cách ngon nhất – 1 lệnh SQL DELETE)
            var rowsAffected = await _db.RefreshTokens
                .Where(t => t.Token == token)
                .ExecuteDeleteAsync();

            return rowsAffected > 0;
        }
        private static string GenerateRefreshToken()
        {
            return Guid.NewGuid().ToString("N");
        }
    }
}
