using Azure.Core;
using DBContexts.OIDC_Management.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using OIDCDemo.AuthorizationServer.Helpers;
using OIDCDemo.AuthorizationServer.Models;
using Renci.SshNet;
using Services.OIDC_Management.Executes;
using Services.OIDC_Management.Executes.AuthorizationClient;
using System.CodeDom.Compiler;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using static Services.OIDC_Management.Executes.AuthorizationClient.AuthorizationClientModel;
using static System.Formats.Asn1.AsnWriter;

namespace OIDCDemo.AuthorizationServer.Controllers
{

    public class AuthorizeController : Controller
    {
        public const int TokenResponseValidSeconds = 1200;
        public const int CodeResponseValidSeconds = 60 * 5;
      
        private readonly ILogger<AuthorizeController> logger;
        private readonly TokenIssuingOptions tokenIssuingOptions;
        private readonly JsonWebKey jsonWebKey;
        private readonly ICodeStorage codeStorage;
        private readonly IRefreshTokenStorageFactory refreshTokenStorageFactory;
        private readonly AuthorizationClientOne authorizationClientOne;
        private readonly AuthorizationClientModel authorizationClientModel;
      

        public AuthorizeController(
         
            TokenIssuingOptions tokenIssuingOptions,
            JsonWebKey jsonWebKey,
            ICodeStorage codeStorage,
            IRefreshTokenStorageFactory refreshTokenStorageFactory,
            AuthorizationClientOne authorizationClientOne,
            ILogger<AuthorizeController> logger,
        AuthorizationClientModel authorizationClientModel)
        {
           
            this.tokenIssuingOptions = tokenIssuingOptions;
            this.jsonWebKey = jsonWebKey;
            this.codeStorage = codeStorage;
            this.refreshTokenStorageFactory = refreshTokenStorageFactory;
            this.authorizationClientOne = authorizationClientOne;
            this.logger = logger;
            this.authorizationClientModel = authorizationClientModel;
        }

        // Hiển thị form login email/password
        // GET /authorize (hiển thị form login)
        // Nếu user đã có cookie SsoAuth hợp lệ -> bỏ qua login, tạo code và trả SubmitForm (redirect về client)
        public async Task<IActionResult> Index(AuthenticationRequestModel authenticateRequest)
        {
            // validate các tham số của client
            ValidateAuthenticateRequestModel(authenticateRequest);

            // 1. Kiểm tra cookie SSO
            var authResult = await HttpContext.AuthenticateAsync("SsoAuth");
            if (authResult?.Succeeded == true && authResult.Principal != null)
            {
                var userId = authResult.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                             ?? authResult.Principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
                var email = authResult.Principal.FindFirst(ClaimTypes.Email)?.Value
                            ?? authResult.Principal.FindFirst(JwtRegisteredClaimNames.Email)?.Value;
                var username = authResult.Principal.FindFirst(ClaimTypes.Name)?.Value
                               ?? authResult.Principal.FindFirst(JwtRegisteredClaimNames.PreferredUsername)?.Value;

                if (!string.IsNullOrEmpty(userId))
                {
                    var sid = authResult.Principal.FindFirst("sid")?.Value;
                    var settings = await authorizationClientOne.GetSetTime();

                    int sessionTime = settings
                        .FirstOrDefault(x => x.Name == "SetSessionTime")
                        ?.Value ?? 8;
                    if (string.IsNullOrEmpty(sid))
                    {
                        // nâng cấp cookie SSO để về sau luôn có sid
                        sid = Guid.NewGuid().ToString("N");
                        var claimsIdentity = new ClaimsIdentity(
                            authResult.Principal.Identity,
                            authResult.Principal.Claims,
                            "SsoAuth",
                            ClaimTypes.Name,
                            ClaimTypes.Role);

                        if (authResult.Principal.FindFirst("sid") == null)
                        {
                            claimsIdentity.AddClaim(new Claim("sid", sid));

                          


                            await HttpContext.SignInAsync("SsoAuth",
                                new ClaimsPrincipal(claimsIdentity),
                                new AuthenticationProperties
                                {
                                    IsPersistent = true,
                                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(sessionTime)
                                });
                        }
                    }

                    // 🔥🔥 LƯU SESSION CHO CLIENT HIỆN TẠI (QUAN TRỌNG)
                    await authorizationClientOne.UseSessionAsync(new AuthorizationClientModel.UserSessionRequest
                    {
                        UserId = userId,
                        ClientId = authenticateRequest.ClientId,   // mỗi client app có ClientId riêng
                        SessionState = sid,
                        ExpiresTime = DateTime.UtcNow.AddHours(sessionTime),
                        IsActive = 1
                    });

                    // Tạo code để client đổi token
                    string code = GenerateAuthenticationCode();
                    var codeValue = new CodeStorageValue()
                    {
                        Code = code,
                        ClientId = authenticateRequest.ClientId,
                        OriginalRedirectUri = authenticateRequest.RedirectUri,
                        ExpiryTime = DateTime.Now.AddSeconds(CodeResponseValidSeconds),
                        Nonce = authenticateRequest.Nonce,
                        User = userId,
                        Email = email,
                        UserName = username,
                        Scope = authenticateRequest.Scope,
                        SessionState = sid
                    };

                    if (!codeStorage.TryAddCode(code, codeValue))
                    {
                        logger.LogError("Failed to store code for silent login (user {u})", userId);
                        ValidateAuthenticateRequestModel(authenticateRequest);
                        return View(authenticateRequest);
                    }

                    logger.LogInformation("Silent-login: issued code {c} for user {u}", code, userId);
                    var codeFlowModel = BuildCodeFlowResponseModel(authenticateRequest, code);

                    return View("SubmitForm", new CodeFlowResponseViewModel()
                    {
                        Code = codeFlowModel.Code,
                        RedirectUri = authenticateRequest.RedirectUri,
                        State = codeFlowModel.State
                    });
                }
            }

            // Không có cookie SSO → show form login
            ValidateAuthenticateRequestModel(authenticateRequest);
            return View(authenticateRequest);
        }


        // Người dùng submit email + password
        [HttpPost]
        public async Task<IActionResult> Authorize(AuthenticationRequestModel authenticateRequest, string email, string password)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ModelState.AddModelError("", "Email và mật khẩu không được bỏ trống");
                ValidateAuthenticateRequestModel(authenticateRequest);
                return View("Index", authenticateRequest);
            }

            // Lấy client từ DB
            var client = await authorizationClientOne.FindByClientId(authenticateRequest.ClientId);
            if (client == null)
            {
                ModelState.AddModelError("", "Tài khoản không hợp lệ");
                ValidateAuthenticateRequestModel(authenticateRequest);
                return View("Index", authenticateRequest);
            }

            if (!client.RedirectUris.Split(';').Contains(authenticateRequest.RedirectUri))
            {
                ModelState.AddModelError("", "Tài khoản không hợp lệ");
                ValidateAuthenticateRequestModel(authenticateRequest);
                return View("Index", authenticateRequest);
            }

            // Xác thực user bằng email/password
            var user = await authorizationClientOne.CheckAccount(email, password);
            if (user == null)
            {
                ModelState.AddModelError("", "Email hoặc mật khẩu không đúng");
                ValidateAuthenticateRequestModel(authenticateRequest);
                return View("Index", authenticateRequest);
            }

            // 🔥 Tạo session_state cho lần đăng nhập này
            string sessionState = Guid.NewGuid().ToString("N");

            // 🔥 Tạo code để user đổi token (chỉ một lần)
            string code = GenerateAuthenticationCode();
            if (!codeStorage.TryAddCode(code, new CodeStorageValue()
            {
                Code = code,
                ClientId = authenticateRequest.ClientId,
                OriginalRedirectUri = authenticateRequest.RedirectUri,
                ExpiryTime = DateTime.Now.AddSeconds(CodeResponseValidSeconds),
                Nonce = authenticateRequest.Nonce,
                User = user.UserId,
                Email = user.Email,
                UserName = user.Username,
                Scope = authenticateRequest.Scope,
                SessionState = sessionState // <--- thêm
            }))
            {
                throw new Exception("Error storing code");
            }
            var settings = await authorizationClientOne.GetSetTime();

            int sessionTime = settings
                .FirstOrDefault(x => x.Name == "SetSessionTime")
                ?.Value ?? 8;
            // 🔥 Đăng nhập SSO cookie (chỉ 1 lần, có claim sid)
            await HttpContext.SignInAsync("SsoAuth", new ClaimsPrincipal(
                new ClaimsIdentity(new[]
                {
            new Claim(ClaimTypes.NameIdentifier, user.UserId),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim("sid", sessionState)
                }, "SsoAuth")),
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(sessionTime)
                });

            // 🔥 LƯU SESSION vào DB
            await authorizationClientOne.UseSessionAsync(new AuthorizationClientModel.UserSessionRequest
            {
                UserId = user.UserId,
                ClientId = authenticateRequest.ClientId,
                SessionState = sessionState,
                ExpiresTime = DateTime.UtcNow.AddHours(sessionTime),
                IsActive = 1
            });

            logger.LogInformation("New authentication code issued: {c}", code);

            // 🔥 Trả về cho user code để đổi token
            return View("SubmitForm", new CodeFlowResponseViewModel()
            {
                Code = code,
                RedirectUri = authenticateRequest.RedirectUri,
                State = authenticateRequest.State,
            });
        }

        [HttpPost("/token")]
        [ResponseCache(NoStore = true)]
        public async Task<IActionResult> GetTokenAsync(string grant_type, string? code, string? refresh_token, string redirect_uri, [FromForm] string client_id, [FromForm] string client_secret)
        {
            if (grant_type == "authorization_code")
            {
                if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(client_id) || string.IsNullOrEmpty(client_secret))
                {
                    return BadRequest("Missing parameters");
                }

                // Lấy code từ storage
                if (!codeStorage.TryGetToken(code, out var codeStorageValue) || codeStorageValue == null)
                {
                    return BadRequest("Invalid code");
                }

                // Kiểm tra redirect_uri
                if (codeStorageValue.OriginalRedirectUri != redirect_uri)
                {
                    return BadRequest("Invalid redirect_uri");
                }

                // Lấy client từ DB
                var client = await authorizationClientOne.FindByClientId(client_id);
                if (client == null)
                {
                    return BadRequest("Invalid client_id");
                }

                // Kiểm tra client_secret
                if (client.ClientSecret != client_secret)
                {
                    return BadRequest("Invalid client_secret");
                }

                codeStorage.TryRemove(code); // code không được dùng lại
                var settings = await authorizationClientOne.GetSetTime();

                int TokenTime = settings
                    .FirstOrDefault(x => x.Name == "SetTokenTime")
                    ?.Value ?? 600;
                // Tạo refresh token

                //var sid = codeStorageValue.SessionState;
                //var userId = codeStorageValue.User;
                //string scope = codeStorageValue.Scope;

                //var refreshToken = await authorizationClientOne.CreateOrReplaceRefreshTokenAsync(
                //userId, client_id, scope);
                //if (refreshToken == null)
                //{
                //    return BadRequest("Không thể cấp refreshToken");
                //}

                //// Trả về Token cho user
                //var result = new AuthenticationResponseModel()
                //{
                //    AccessToken = GenerateAccessToken(codeStorageValue, codeStorageValue.User, codeStorageValue.Scope, client.ClientId, codeStorageValue.Nonce, jsonWebKey),
                //    IdToken = GenerateIdToken(codeStorageValue, codeStorageValue.User, client.ClientId, codeStorageValue.Nonce, jsonWebKey),
                //    TokenType = "Bearer",
                //    RefreshToken = refreshToken.Token,
                //    ExpiresIn = TokenResponseValidSeconds,

                //};

                //logger.LogInformation("access_token: {t}", result.AccessToken);
                //logger.LogInformation("refresh_token: {t}", result.RefreshToken);

                //return Json(result);


                //var sid = codeStorageValue.SessionState;
                var sid = string.IsNullOrEmpty(codeStorageValue.SessionState)
    ? Guid.NewGuid().ToString("N")
    : codeStorageValue.SessionState;

                // Tạo refresh token
                var userId = codeStorageValue.User;
                string scope = codeStorageValue.Scope;
              
                var refreshToken = await authorizationClientOne.CreateOrReplaceRefreshTokenAsync(userId, client_id, scope);
                if (refreshToken == null) return BadRequest("Không thể cấp refreshToken");

                var result = new AuthenticationResponseModel()
                {
                    AccessToken = GenerateAccessToken(codeStorageValue, userId, scope, client.ClientId, codeStorageValue.Nonce, sid, jsonWebKey),
                    IdToken = GenerateIdToken(codeStorageValue, userId, client.ClientId, codeStorageValue.Nonce, sid, jsonWebKey),
                    TokenType = "Bearer",
                    RefreshToken = refreshToken.Token,
                    ExpiresIn = TokenTime,
                };

                return Json(result);
            }
            // 2. Refresh Token Flow
            if (grant_type == "refresh_token")
            {
                if (string.IsNullOrEmpty(refresh_token) ||
                    string.IsNullOrEmpty(client_id) ||
                    string.IsNullOrEmpty(client_secret))
                    return BadRequest("invalid_request");

                // 1) Validate client
                var client = await authorizationClientOne.FindByClientId(client_id);
                if (client == null || client.ClientSecret != client_secret)
                    return BadRequest("invalid_client");

                // 2) Tìm refresh token trong DB
                var rt = await authorizationClientOne.FindRefreshTokenAsync(refresh_token, client_id);


                if (rt == null || rt.ExpiresTime <= DateTime.UtcNow)
                {
                    // Thu hồi nếu đã hết hạn hoặc không hợp lệ
                    if (rt != null) await authorizationClientOne.RevokeTokenAsync(refresh_token);
                    return BadRequest("invalid_grant");
                }

                // 3) Kiểm tra user còn session active không (bị global logout thì chặn)
                if (!await authorizationClientOne.IsAnySessionActiveAsync(rt.UserId))
                {
                    await authorizationClientOne.RevokeTokenAsync(refresh_token);
                    return BadRequest("session_revoked"); // hoặc invalid_grant
                }

                // 4) Lấy sid đang active nhất cho user + client này (nếu muốn đưa vào token mới)
                var sid = await authorizationClientOne.GetLatestActiveSidAsync(rt.UserId, client_id) ?? Guid.NewGuid().ToString("N");

                // 5) Lấy user
                var userEntity = await authorizationClientOne.FindByUserId(rt.UserId);
                if (userEntity == null) return BadRequest("invalid_grant");

                // 6) Refresh token rotation
                var newRefreshToken = await authorizationClientOne.CreateOrReplaceRefreshTokenAsync(
                    rt.UserId, client_id, rt.Scope, refresh_token);

                if (newRefreshToken == null) return BadRequest("invalid_refreshToken");

                // 7) Tạo access_token/id_token mới
                // Tạo CodeStorageValue ảo để tái dùng hàm generate (hoặc tạo hàm generate nhận Email/Username rời)
                var tmp = new CodeStorageValue
                {
                    Code = Guid.NewGuid().ToString("N"),
                    ClientId = client_id,
                    OriginalRedirectUri = redirect_uri,
                    ExpiryTime = DateTime.UtcNow.AddMinutes(5),
                    User = rt.UserId,
                    Email = userEntity.Email,
                    UserName = userEntity.UserName,
                    Scope = rt.Scope,
                    Nonce = "", // không cần cho refresh flow
                    SessionState = sid
                };


                var newAccessToken = GenerateAccessToken(tmp, rt.UserId, rt.Scope, client.ClientId, tmp.Nonce, sid, jsonWebKey);
                var newIdToken = GenerateIdToken(tmp, rt.UserId, client.ClientId, tmp.Nonce, sid, jsonWebKey);

                return Ok(new
                {
                    access_token = newAccessToken,
                    id_token = newIdToken,
                    token_type = "Bearer",
                    expires_in = tokenIssuingOptions.AccessTokenExpirySeconds,
                    refresh_token = newRefreshToken.Token
                });
            }




            return BadRequest("unsupported_grant_type");
        }




        private string GenerateIdToken(CodeStorageValue user, string userId, string audience, string nonce, string sid, JsonWebKey jsonWebKey)
        {
            var claims = new List<Claim>
    {
        new(JwtRegisteredClaimNames.Sub, userId),
        new(JwtRegisteredClaimNames.Email, user.Email),
        new(JwtRegisteredClaimNames.PreferredUsername, user.UserName),
        new("sid", sid) // <--- thêm
    };

            return JwtGenerator.GenerateJWTToken(
                tokenIssuingOptions.IdTokenExpirySeconds,
                tokenIssuingOptions.Issuer,
                audience,
                nonce,
                claims,
                jsonWebKey
            );
        }

        private string GenerateAccessToken(CodeStorageValue user, string userId, string scope, string audience, string nonce, string sid, JsonWebKey jsonWebKey)
        {
            var claims = new List<Claim>
    {
        new(JwtRegisteredClaimNames.Sub, userId),
        new(JwtRegisteredClaimNames.Email, user.Email),
        new(JwtRegisteredClaimNames.PreferredUsername, user.UserName),
        new("scope", scope),
        new("sid", sid) // <--- thêm
    };

            return JwtGenerator.GenerateJWTToken(
                tokenIssuingOptions.AccessTokenExpirySeconds,
                tokenIssuingOptions.Issuer,
                audience,
                nonce,
                claims,
                jsonWebKey
            );
        }

        private static string GenerateAuthenticationCode()
        {
            return Guid.NewGuid().ToString("N");
        }

        public IActionResult Cancel(AuthenticationRequestModel authenticateRequest)
        {
            return View();
        }

        private static void ValidateAuthenticateRequestModel(AuthenticationRequestModel authenticateRequest)
        {
            ArgumentNullException.ThrowIfNull(authenticateRequest, nameof(authenticateRequest));

            if (string.IsNullOrEmpty(authenticateRequest.ClientId))
            {
                throw new Exception("client_id required");
            }

            if (string.IsNullOrEmpty(authenticateRequest.ResponseType))
            {
                throw new Exception("response_type required");
            }

            if (string.IsNullOrEmpty(authenticateRequest.Scope))
            {
                throw new Exception("scope required");
            }

            if (string.IsNullOrEmpty(authenticateRequest.RedirectUri))
            {
                throw new Exception("redirect_uri required");
            }
        }


        private static CodeFlowResponseModel BuildCodeFlowResponseModel(AuthenticationRequestModel authenticateRequest, string code)
        {
            return new CodeFlowResponseModel()
            {
                Code = code,
                State = authenticateRequest.State
            };
        }


        // (Tuỳ chọn) Xem danh sách session để debug
        [Authorize]
        [HttpGet("/sessions")]
        public async Task<IActionResult> GetMySessions()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var sessions = await authorizationClientOne.GetSessionsForUserAsync(userId);

            var result = sessions.Select(s => new
            {
                s.ClientId,
                s.SessionState,
                s.IsActive,
                s.CreatedTime,
                s.LastAccessTime,
                s.ExpiresTime
            });

            return Json(result);
        }




    }
}