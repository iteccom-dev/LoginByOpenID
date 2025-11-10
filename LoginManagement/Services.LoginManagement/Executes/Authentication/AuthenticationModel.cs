namespace Services.LoginManagement.Executes.Authentication
{
    public class AuthenticationModel
    {
        public class AuthenRequest
        {
            public string Email { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }

        public class AuthenResponse
        {
            public string AccessToken { get; set; } = string.Empty;
            public string RefreshToken { get; set; } = string.Empty;
            public string IdToken { get; set; } = string.Empty;
            public string TokenType { get; set; } = "Bearer";
            public DateTime AccessTokenExpiry { get; set; }
            public DateTime RefreshTokenExpiry { get; set; }
            public int ExpiresIn { get; set; }
        }

        public class ApiResponse
        {
            public bool Success { get; set; }
            public string Message { get; set; } = string.Empty;
            public AuthenResponse? AccountResponse { get; set; }
        }
    }
}
