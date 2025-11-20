using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.OIDC_Management.Executes
{
  public class ClientModel
    {
        public class ClientResponse
        {
            public string ClientId { get; set; }
            public string ClientSecret { get; set; }
            public string DisplayName { get; set; }

            public string RedirectUris { get; set; }
            public string CallbackPath { get; set; }
            public string AccessDeniedPath { get; set; }
            public string Scope { get; set; }
            public string GrantType { get; set; }
            public string Authority { get; set; }
            public string KeyWord { get; set; }

            public int? Status { get; set; }
            public int? CreatedBy { get; set; }
            public DateTime? CreatedDate { get; set; }
            public int? UpdatedBy { get; set; }
            public DateTime? UpdatedDate { get; set; }
        }


        public class ClientFilterRequest
        {
            public int Page { get; set; } = 1;
            public int PageSize { get; set; } = 10;

            public string? KeySearch { get; set; }
            public int? Status { get; set; } = 1;

            public DateTime? CreatedFrom { get; set; }
            public DateTime? CreatedTo { get; set; }
        }
        public class ClientListResponse
        {
            public List<ClientResponse> Items { get; set; }
            public int TotalRecords { get; set; }
            public int Page { get; set; }
            public int PageSize { get; set; }
        }


        public class DeleteClientRequest
        {
            public string Id { get; set; }
        }


        public class ClientRequest
        {
            public string ClientId { get; set; }
            public string DisplayName { get; set; }
            public string RedirectUris { get; set; }
            public string CallbackPath { get; set; }
            public string AccessDeniedPath { get; set; }
            public string Scope { get; set; }
            public string GrantType { get; set; }
            public string Authority { get; set; }
            public string Keyword { get; set; }
            public int? Status { get; set; }
        }

    }
}
