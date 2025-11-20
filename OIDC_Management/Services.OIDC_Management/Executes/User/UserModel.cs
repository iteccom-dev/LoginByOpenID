using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.OIDC_Management.Executes
{
    public class UserModel
    {
        public class FilterListRequest
        {
            public int Page { get; set; } = 1;
            public int PageSize { get; set; } = 5;
            public string ClientId { get; set; }
            public string? KeySearch { get; set; }
        }
        public class UserResponse
        {
            public string? Id { get; set; }

            public string? UserName { get; set; }

            public string? Email { get; set; }

            public string? PhoneNumber { get; set; }

            public int? Status { get; set; }

            public string? ClientId { get; set; }
        }
        public class UserListResponse
        {
            public List<UserResponse> Items { get; set; } = new();
            public int TotalRecords { get; set; }
            public int CurrentPage { get; set; }
            public int PageSize { get; set; }
        }
    }
}
