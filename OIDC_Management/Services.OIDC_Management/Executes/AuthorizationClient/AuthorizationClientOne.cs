using DBContexts.OIDC_Management.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq;
using Services.OIDC_Management.Helpers;
using System;
using System.Collections.Generic;
using System.Globalization;
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
        public async Task<List<Setting>> GetSetTime()
        {
            return await _db.Settings.ToListAsync();
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

        /// <summary>
        /// Tìm user theo email, nếu không có thì tạo mới (cho MS365 login)
        /// </summary>
        public async Task<AspNetUser?> FindOrCreateUserByEmailAsync(string email, string userName)
        {
            var user = await _db.AspNetUsers.FirstOrDefaultAsync(x => x.Email == email);
            
            if (user != null)
                return user;

            // Tạo user mới nếu chưa có
            user = new AspNetUser
            {
                Id = Guid.NewGuid().ToString(),
                Email = email,
                UserName = userName,
                NormalizedEmail = email.ToUpperInvariant(),
                NormalizedUserName = userName.ToUpperInvariant(),
                EmailConfirmed = true,
                SecurityStamp = Guid.NewGuid().ToString(),
                ConcurrencyStamp = Guid.NewGuid().ToString(),
                Status = 1 // Active
            };

            _db.AspNetUsers.Add(user);
            await _db.SaveChangesAsync();

            return user;
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



        //Bắt đầu cái đăng xuát từ đây

        // Lấy ra session cụ thể của user theo userId + clientId + session_state
        public async Task<UserSession?> GetSessionAsync(string userId, string clientId, string state)
        {
            var now = DateTime.UtcNow;
            return await _db.UserSessions
                .Include(x => x.RefreshTokens)
                .FirstOrDefaultAsync(x =>
                    x.SessionState == state &&
                    x.UserId == userId &&
                    x.ClientId == clientId &&
                    x.IsActive == 1 &&
                    (x.ExpiresTime == null || x.ExpiresTime > now));
        }

        // Tạo mới hoặc cập nhật session cho user
        public async Task<UserSession> UseSessionAsync(UserSessionRequest req)
        {
            try
            {
                var now = DateTime.UtcNow;
                var entity = await _db.UserSessions
                    .FirstOrDefaultAsync(x => x.UserId == req.UserId
                                           && x.ClientId == req.ClientId
                                           && x.SessionState == req.SessionState);

                if (entity == null)
                {
                    entity = new UserSession
                    {
                        UserId = req.UserId,
                        ClientId = req.ClientId,
                        SessionState = req.SessionState,
                        CreatedTime = now,
                        LastAccessTime = now,
                        ExpiresTime = req.ExpiresTime == default ? now.AddDays(30) : req.ExpiresTime,
                        IsActive = 1
                    };
                    _db.UserSessions.Add(entity);
                }
                else
                {
                    entity.IsActive = 1;
                    entity.LastAccessTime = now;
                    if (req.ExpiresTime != default) entity.ExpiresTime = req.ExpiresTime;
                }

                await _db.SaveChangesAsync();
                Console.WriteLine($"[DEBUG] Saved session for user={req.UserId}, client={req.ClientId}");
                return entity;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] UseSessionAsync failed: {ex.Message}");
                throw;
            }
        }

        // dừng kích hoạt (logout) đúng MỘT session dựa vào session_state
        public async Task<int> DeactivateSessionAsync(string userId, string state)
        {
            var now = DateTime.UtcNow;

            var sessions = await _db.UserSessions
                .Where(x => x.UserId == userId && x.SessionState == state && x.IsActive == 1)
                .ToListAsync();

            foreach (var s in sessions)
            {
                s.IsActive = 0;
                s.LastAccessTime = now;
            }

            var affected = await _db.SaveChangesAsync();

            if (affected > 0)
            {
                var affectedClientIds = sessions.Select(x => x.ClientId).Distinct().ToList();
                if (affectedClientIds.Any())
                {
                    await _db.RefreshTokens
                        .Where(t => t.UserId == userId && affectedClientIds.Contains(t.ClientId))
                        .ExecuteDeleteAsync();
                }
            }

            return affected;
        }

        // - Xóa toàn bộ refresh token của user → không app nào có thể refresh token nữa
        public async Task<int> DeactivateAllSessionsForUserAsync(string userId)
        {
            var now = DateTime.UtcNow;

            var sessions = await _db.UserSessions
                .Where(x => x.UserId == userId && x.IsActive == 1)
                .ToListAsync();

            foreach (var s in sessions)
            {
                s.IsActive = 0;
                s.LastAccessTime = now;
            }

            var affected = await _db.SaveChangesAsync();

            if (affected > 0)
            {
                await _db.RefreshTokens
                    .Where(t => t.UserId == userId)
                    .ExecuteDeleteAsync();
            }

            return affected;
        }

        //Kiểm tra user có session nào còn hoạt động không
         public async Task<bool> IsAnySessionActiveAsync(string userId)
        {
            var now = DateTime.UtcNow;
            return await _db.UserSessions.AnyAsync(x =>
                x.UserId == userId &&
                x.IsActive == 1 &&
                (x.ExpiresTime == null || x.ExpiresTime > now));
        }

        // Lấy SID (session_state) mới nhất còn active của user trong 1 client
        public async Task<string?> GetLatestActiveSidAsync(string userId, string clientId)
        {
            var now = DateTime.UtcNow;
            return await _db.UserSessions
                .Where(x => x.UserId == userId &&
                            x.ClientId == clientId &&
                            x.IsActive == 1 &&
                            (x.ExpiresTime == null || x.ExpiresTime > now))
                .OrderByDescending(x => x.LastAccessTime)
                .Select(x => x.SessionState)
                .FirstOrDefaultAsync();
        }

        // Lấy danh sách session của user
        public async Task<List<UserSession>> GetSessionsForUserAsync(string userId)
        {
            return await _db.UserSessions
                .Include(s => s.Client)     // ⭐ BẮT BUỘC
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.LastAccessTime)
                .ToListAsync();
        }

        // Xóa toàn bộ refresh token của user
        public async Task<int> RevokeAllTokensForUserAsync(string userId)
        {
            return await _db.RefreshTokens.Where(t => t.UserId == userId).ExecuteDeleteAsync();
        }

        // Tìm refresh token theo token + clientId
        public async Task<RefreshToken?> FindRefreshTokenAsync(string token, string clientId)
        {
            return await _db.RefreshTokens
                .FirstOrDefaultAsync(t => t.Token == token && t.ClientId == clientId);
        }



        public async Task<bool> CheckSessionAsync(string sid)
        {
            var now = DateTime.UtcNow;

            var session = await _db.UserSessions
                .FirstOrDefaultAsync(x =>
                    x.SessionState == sid &&
                    x.IsActive == 1 &&
                    (x.ExpiresTime == null || x.ExpiresTime > now));

            return session != null;
        }

    }
}
