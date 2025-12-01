using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.OIDC_Management.Executes.AuthorizationClient
{
    public class AuthorizationClientModel
    {
        public class AuthorizationClient
        {
            public int Id { get; set; }
            public string ClientId { get; set; } = string.Empty;
            public string ClientSecret { get; set; } = string.Empty;
            public string RedirectUri { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string GrantTypes { get; set; } = string.Empty; // e.g. "authorization_code refresh_token"
            public string Scopes { get; set; } = string.Empty;     // e.g. "openid profile read write"
            public int Status { get; set; } = 1;
        }

        public class AuthenticationRequestModel
        {
            [BindProperty(Name = "client_id", SupportsGet = true)]
            public string ClientId { get; set; } = string.Empty;
            //[BindProperty(Name = "client_secret", SupportsGet = true)]
            //public string ClientSecret { get; set; } = string.Empty;
            [BindProperty(Name = "redirect_uri", SupportsGet = true)]
            public string RedirectUri { get; set; } = string.Empty;

            [BindProperty(Name = "returnUrl", SupportsGet = true)]
            public string? ReturnUrl { get; set; }

            [BindProperty(Name = "response_type", SupportsGet = true)]
            public string ResponseType { get; set; } = string.Empty;
            [BindProperty(Name = "scope", SupportsGet = true)]
            public string Scope { get; set; } = string.Empty;
            [BindProperty(Name = "code_challenge", SupportsGet = true)]
            public string CodeChallenge { get; set; } = string.Empty;
            [BindProperty(Name = "code_challenge_method", SupportsGet = true)]
            public string CodeChallengeMethod { get; set; } = string.Empty;
            [BindProperty(Name = "response_mode", SupportsGet = true)]
            public string ResponseMode { get; set; } = string.Empty;
            [BindProperty(Name = "nonce", SupportsGet = true)]
            public string Nonce { get; set; } = string.Empty;
            [BindProperty(Name = "state", SupportsGet = true)]
            public string State { get; set; } = string.Empty;

            [BindProperty(Name = "email", SupportsGet = false)]
            public string? Email { get; set; }

            [BindProperty(Name = "password", SupportsGet = false)]
            public string? Password { get; set; }
        }
        public class AuthUserInfo
        {
            public string UserId { get; set; }
            public string Username { get; set; }
            public string Email { get; set; }
        }
    }
}
