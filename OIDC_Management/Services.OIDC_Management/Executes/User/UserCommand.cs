using DBContexts.OIDC_Management.Entities;
using Microsoft.EntityFrameworkCore;
using Services.OIDC_Management.Helpers;
using static Services.OIDC_Management.Executes.UserModel;

public class UserCommand
{
    private readonly oidcIdentityContext _context;

    public UserCommand(oidcIdentityContext context)
    {
        _context = context;
    }

    public async Task<int> Create(UserRequest request)
    {
        var emailExists = _context.AspNetUsers
            .Any(u => u.ClientId == request.UserClient && u.Email == request.UserEmail);

        if (emailExists)
        {
            return 0;
        }

        string salt = PasswordHelper.GenerateSalt();
        string id = Guid.NewGuid().ToString();

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

        await _context.AspNetUsers.AddAsync(newUser);

        var newUserLogin = new AspNetUserLogin
        {
            LoginProvider = "OpenIdConnect",
            ProviderKey = id,
            ProviderDisplayName = "OpenIdConnect",
            UserId = id
        };
        await _context.AspNetUserLogins.AddAsync(newUserLogin);

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
        if (string.IsNullOrEmpty(request.Id)) return 0;

        var user = await _context.AspNetUsers
            .FirstOrDefaultAsync(u => u.Id == request.Id);

        if (user == null) return 0;

         if (!string.IsNullOrWhiteSpace(request.UserPassword))
        {
            user.SecurityStamp = PasswordHelper.GenerateSalt();
            user.PasswordHash = PasswordHelper.HashPassword(request.UserPassword, user.SecurityStamp);
        }

        user.UserName = request.UserName;
        user.Email = request.UserEmail;
        user.PhoneNumber = request.UserPhone;
        user.Status = request.UserStatus;
        user.ClientId = request.UserClient;

        return await _context.SaveChangesAsync();
    }


    public async Task<int> AddUser(List<UserRequest> users)
    {
        int count = 0;

        foreach (var user in users)
        {
            if (await IsUserExists(user.Id)) // dùng await
            {
                // Nếu đã tồn tại -> cập nhật
                int updated = await Update(user);
                if (updated > 0) count++;
            }
            else
            {
                // Nếu chưa tồn tại -> thêm mới
                int inserted = await Create(user);
                if (inserted > 0) count++;
            }
        }

        return count;
    }

    // Kiểm tra user có tồn tại
    private async Task<bool> IsUserExists(string id)
    {
        if (string.IsNullOrEmpty(id)) return false;
        return await _context.AspNetUsers.AnyAsync(u => u.Id == id && u.Status != -1);
    }

    public async Task<bool> SetTime(int? sTime, int? rtTime)
    {
      

        // Nếu có giá trị được truyền vào thì update
        if (sTime.HasValue)
        {
            var setting = await _context.Settings.FirstOrDefaultAsync(p => p.Section == "SetSessionTime");

            setting.Value = sTime.Value.ToString();
        }

        if (rtTime.HasValue)
        {
            var setting = await _context.Settings.FirstOrDefaultAsync(p => p.Section == "SetTokenTime");

            setting.Value = rtTime.Value.ToString();
        }

        // Lưu thay đổi vào DB
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> SetLogo(string? urlLogoMain, string? urlLogoSub)
    {


        // Nếu có giá trị được truyền vào thì update
        if (!string.IsNullOrWhiteSpace(urlLogoMain))
        {
            var setting = await _context.Settings.FirstOrDefaultAsync(p => p.Section == "MainLogo");

            setting.Value = urlLogoMain;
            setting.UpdateDate = DateTime.Now;
        }

        if (!string.IsNullOrWhiteSpace(urlLogoSub))
        {
            var setting = await _context.Settings.FirstOrDefaultAsync(p => p.Section == "SubLogo");

            setting.Value = urlLogoSub;
            setting.UpdateDate = DateTime.Now;
        }

        // Lưu thay đổi vào DB
        await _context.SaveChangesAsync();

        return true;
    }



}
