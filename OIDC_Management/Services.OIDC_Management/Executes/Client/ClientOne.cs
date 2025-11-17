using DBContexts.OIDC_Management.Entities;
using Microsoft.EntityFrameworkCore;
using Services.OIDC_Management.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.OIDC_Management.Executes
{
    public class ClientOne
    {
        private readonly oidcIdentityContext _db;

        public ClientOne(oidcIdentityContext db)
        {
            _db = db;
        }
        public async Task<bool> CheckAccount(string email, string password)
        {
            var isValid = await _db.AspNetUsers.FirstOrDefaultAsync(x => x.Email == email);
            if (isValid == null)
            {
                return false;
            }
            bool result = PasswordHelper.VerifyPassword(password, isValid.SecurityStamp, isValid.PasswordHash);
            return result;

        }
    }
}

