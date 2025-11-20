using DBContexts.OIDC_Management.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Services.OIDC_Management.Executes.UserModel;
using static Services.OIDC_Management.Executes.UserModel.ClientResponse;

namespace Services.OIDC_Management.Executes
{
    public class UserMany
    {
        private readonly oidcIdentityContext _context;

        public UserMany(oidcIdentityContext context)
        {
            _context = context;
        }
        public async Task<UserListResponse> GetMany(FilterListRequest filter)
        {
            IQueryable<AspNetUser> query = _context.AspNetUsers
               .Where(p => p.Status >= 0);

            // Search keyword
            if (!string.IsNullOrWhiteSpace(filter.KeySearch))
            {
                string keyword = filter.KeySearch.Trim();
                string collate = "Vietnamese_CI_AI";
                query = query.Where(x =>
                       EF.Functions.Collate(x.UserName ?? "", collate).Contains(keyword) ||
                       EF.Functions.Collate(x.Email ?? "", collate).Contains(keyword) ||
                       EF.Functions.Collate(x.PhoneNumber ?? "", collate).Contains(keyword));
              
            }

            // Filter CliendId
            if (!string.IsNullOrWhiteSpace(filter.ClientId))
            {
                query = query.Where(p => p.ClientId == filter.ClientId);
            }

            // Count total records
            int totalRecords = await query.CountAsync();

            // Paging
            int page = filter.Page <= 0 ? 1 : filter.Page;
            int PAGE_SIZE = /*filter.PageSize*/2;

            var results = await query
           
            .Skip((page - 1) * PAGE_SIZE)
            .Take(PAGE_SIZE)
            .Select(e => new UserResponse
            {
                Id = e.Id,
                UserName = e.UserName,
                Email = e.Email,
                PhoneNumber = e.PhoneNumber,
                ClientId = e.ClientId,
                Status = e.Status
              
            })
            .ToListAsync();

            return new UserListResponse
            {
                Items = results,
                TotalRecords = totalRecords,
                CurrentPage = page,
                PageSize = PAGE_SIZE
            };
        }

    }
}
