using DBContexts.OIDC_Management.Entities;
using Microsoft.EntityFrameworkCore;
using static Services.OIDC_Management.Executes.ClientModel;

namespace Services.OIDC_Management.Executes
{
    public class ClientMany
    {
        private readonly oidcIdentityContext _db;

        public ClientMany(oidcIdentityContext db)
        {
            _db = db;
        }
        public async Task<List<ClientIdList>> GetMany()
        {
            return await _db.Clients
                .Select(x => new ClientIdList
                {
                    Id = x.ClientId,
                    Name = x.DisplayName,
                   
                })
                .ToListAsync();
        }
        public async Task<ClientModel.ClientListResponse> Gets(ClientModel.ClientFilterRequest filter)
        {
            IQueryable<Client> query = _db.Clients
                .Where(x => x.Status >= 0);

            // Search
            if (!string.IsNullOrWhiteSpace(filter.KeySearch))
            {
                string keyword = filter.KeySearch.Trim();
                string collate = "Vietnamese_CI_AI";

                query = query.Where(x =>
                    EF.Functions.Collate(x.ClientId ?? "", collate).Contains(keyword) ||
                    EF.Functions.Collate(x.DisplayName ?? "", collate).Contains(keyword) ||
                    EF.Functions.Collate(x.KeyWord ?? "", collate).Contains(keyword)
                );
            }

            // Filter status
            if (filter.Status.HasValue)
                query = query.Where(x => x.Status == filter.Status.Value);

            // Date range
            if (filter.CreatedFrom.HasValue)
                query = query.Where(x => x.CreatedDate >= filter.CreatedFrom.Value);

            if (filter.CreatedTo.HasValue)
            {
                var end = filter.CreatedTo.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(x => x.CreatedDate <= end);
            }

            // Count
            int totalRecords = await query.CountAsync();

            // Paging
            int page = filter.Page <= 0 ? 1 : filter.Page;
            int pageSize = filter.PageSize;

            var items = await query
                .OrderByDescending(x => x.CreatedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new ClientModel.ClientResponse
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

            return new ClientModel.ClientListResponse
            {
                Items = items,
                TotalRecords = totalRecords,
                Page = page,
                PageSize = pageSize
            };
        }
    }

  
}
