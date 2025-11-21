using DBContexts.OIDC_Management.Entities;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Crypto.Generators;
using Services.OIDC_Management.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Services.OIDC_Management.Executes.UserModel;
using static Services.OIDC_Management.Executes.UserModel.ClientResponse;

namespace Services.OIDC_Management.Executes
{
    public class UserCommand
    {
        private readonly oidcIdentityContext _context;

        public UserCommand(oidcIdentityContext context)
        {
            _context = context;
        }
        public async Task<int> Create(UserRequest request)
        {
            // 1. Kiểm tra email đã tồn tại cho client chưa
            var emailExists = _context.AspNetUsers
                .Any(u => u.ClientId == request.UserClient && u.Email == request.UserEmail);

            if (emailExists)
            {
                // Email đã tồn tại → trả về 0
                return 0;
            }
            string salt = PasswordHelper.GenerateSalt();
            string id = Guid.NewGuid().ToString();
            // 2. Tạo entity mới từ request
            var newUser = new AspNetUser
            {
                Id = id,
                UserName = request.UserName,
                PasswordHash = PasswordHelper.HashPassword(request.UserPassword, salt),
                Email = request.UserEmail,
                PhoneNumber = request.UserPhone,
                SecurityStamp = salt,
                ConcurrencyStamp = Guid.NewGuid().ToString(),
                EmailConfirmed = true,
                LockoutEnabled = true,
                AccessFailedCount = 0,

                Status = request.UserStatus,

                ClientId = request.UserClient,
               


            };
            // 3. Thêm vào DbContext
            await _context.AspNetUsers.AddAsync(newUser);
            var newUserLogin = new AspNetUserLogin
            {
                LoginProvider = "OpenIdConnect",
                ProviderKey = id,
                ProviderDisplayName = "OpenIdConnect",
                UserId = id
            };
            await _context.AspNetUserLogins.AddAsync(newUserLogin);

            // 4. Lưu thay đổi
            var saved = await _context.SaveChangesAsync();

           
            return saved;
        }
        public async Task<int> Delete(string id)
        {
            var user = await _context.AspNetUsers.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return 0;

            user.Status = -1; 
            return await _context.SaveChangesAsync();
        }

        public async Task<int> Update(UserRequest request)
        {
            if (request.Id == null) return 0;
            var user = await _context.AspNetUsers
                .FirstOrDefaultAsync(u => u.Id == request.Id);

            if (user == null)
                return 0;

            user.UserName = request.UserName;
            user.Email = request.UserEmail;
            user.PhoneNumber = request.UserPhone;
            user.Status = request.UserStatus;

            return await _context.SaveChangesAsync();
        }



    }
}
