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
        public async Task<string?> CheckAccount(string email, string password)
        {
            var isValid = await _db.AspNetUsers.FirstOrDefaultAsync(x => x.Email == email);
            if (isValid == null)
            {
                return null;
            }
            bool result = PasswordHelper.VerifyPassword(password, isValid.SecurityStamp, isValid.PasswordHash);
            if (result == false)
            {
                return null;
            }
            return isValid.Id;

        }

    }
}
