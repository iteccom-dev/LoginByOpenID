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
}
