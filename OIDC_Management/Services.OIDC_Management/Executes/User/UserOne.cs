using DBContexts.OIDC_Management.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Services.OIDC_Management.Executes.UserModel;

namespace Services.OIDC_Management.Executes
{
    public class UserOne
    {
        private readonly oidcIdentityContext _context;

        public UserOne(oidcIdentityContext context)
        {
            _context = context;
        }

        public async Task<List<UserResponse>> Get(string id)
        {
        var query = _context.AspNetUsers
              
               .Where(e => e.Status >= 0 && e.Id == id);

            var result = await query
                .Select(e => new UserResponse
                {
                    Id = e.Id,
                    UserName = e.UserName,
                    PhoneNumber = e.PhoneNumber,
                    Email = e.Email,
                    Status = e.Status,
                  
                })
                .ToListAsync();

            return result;
        }
        public async Task<List<Setting>> GetSetTime()
        {
            return await _context.Settings.ToListAsync();
        }


    }
}
