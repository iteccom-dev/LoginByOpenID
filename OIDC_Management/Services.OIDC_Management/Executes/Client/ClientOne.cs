using DBContexts.OIDC_Management.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Services.OIDC_Management.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Services.OIDC_Management.Executes.ClientModel;

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

        public async Task<List<ClientResponse>> Get(string? clientId = null)
        {
            IQueryable<Client> query = _db.Clients.AsNoTracking()
                .Where(c => c.Status >= 0);

            if (!string.IsNullOrWhiteSpace(clientId))
            {
                query = query.Where(c => c.ClientId == clientId);
            }

            var result = await query
                .Select(c => new ClientResponse
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
                })
                .ToListAsync();

            return result;
        }



    }
}
