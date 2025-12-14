using DBContexts.OIDC_Management.Entities;
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
         
            public int? Status { get; set; } = 1;
            public string? KeySearch { get; set; }
        }
        public class UserResponse
        {
            public string? Id { get; set; }

            public string? UserName { get; set; }

            public string? Email { get; set; }

            public string? PhoneNumber { get; set; }

            public int? Status { get; set; } = 1;

            public string? ClientId { get; set; }
            public string? Password { get; set; }
            public bool HasPassword { get; set; }

            public List<ClientResponse> Clients { get; set; } = new List<ClientResponse>();
        }
         
        public class SettingTime
        {
            public int SessionTime { get; set; } = 8;
            public int RefreshTokenTime { get; set; } = 600;
        }
        public class ClientResponse
        {
            public string Id { get; set; }
            public string Name { get; set; }  // thêm property này
        }
        public class UserListResponse
            {
                public List<UserResponse> Items { get; set; } = new();
                public int TotalRecords { get; set; }
                public int CurrentPage { get; set; }
                public int PageSize { get; set; }
            }
        public class ExternalUser
        {
            public string microsoftId { get; set; }
            public string email { get; set; }
            public string fullName { get; set; }
            public string password { get; set; }
            // Những trường khác có thể bỏ qua
        }
        public class UserRequest
            {
                public string? Id { get; set; } = string.Empty;
                public string? UserName { get; set; } = string.Empty;
                public string? UserPassword { get; set; } = string.Empty;
                public string? UserEmail { get; set; } = string.Empty;
                public string? UserPhone { get; set; } = string.Empty;

                //public string? UserRoles { get; set; } = string.Empty;
                public int UserStatus { get; set; } = 1;
                public string? UserProvider { get; set; } = string.Empty;
                public string? UserClient { get; set; } = string.Empty;
                public bool? User2FA { get; set; } = false;
            public int? Role { get; set; }


            public string? CreatedBy { get; set; } = string.Empty;
                public string? UpdatedBy { get; set; } = string.Empty;
                public string? CreatedAt { get; set; } = string.Empty;
                public string? UpdatedAt { get; set; } = string.Empty;
            }


        
    }
}
