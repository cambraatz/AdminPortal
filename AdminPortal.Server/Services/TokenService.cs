using AdminPortal.Server.Services.Interfaces;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace AdminPortal.Server.Services
{
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _config;
        private readonly JwtSecurityTokenHandler _handler = new();
        private readonly ILogger<TokenService> _logger;
        private readonly ICookieService _cookieService;
        public TokenService(IConfiguration config, ILogger<TokenService> logger, ICookieService cookieService)
        {
            _config = config;
            _logger = logger;
            _cookieService = cookieService;
        }

        /* Token Generation
         *  creates Jwt Security Tokens to maintain 
         *  authorization throughout the session...
         */
        public (string accessToken, string refreshToken) GenerateToken(string username)
        {
            var now = DateTimeOffset.UtcNow;

            List<Claim> baseClaims= new List<Claim>
            {
                new Claim(ClaimTypes.Name, username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var configuredAudiencesString = _config["Jwt:Audience"];
            var audiences = configuredAudiencesString?
                                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                                .Select(a => a.Trim())
                                .ToList();

            if (audiences == null || audiences.Count == 0)
            {
                _logger.LogError("Jwt:Audience configuration is missing or empty. Token will be issued without audiences.");
            }
            else
            {
                foreach (var aud in audiences)
                {
                    baseClaims.Add(new Claim(JwtRegisteredClaimNames.Aud, aud));
                }
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var access = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                claims: baseClaims,
                expires: now.AddMinutes(15).UtcDateTime,
                signingCredentials: creds);

            var refresh = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                claims: baseClaims,
                expires: now.AddDays(1).UtcDateTime,
                signingCredentials: creds);

            return (_handler.WriteToken(access), _handler.WriteToken(refresh));
        }

        /* Token Validation
         *  validates Jwt Security Token 
         */
        public TokenValidation ValidateTokens(string accessToken, string refreshToken, string username, bool tryRefresh = true)
        {
            var tokenParams = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = _config["Jwt:Issuer"],

                ValidateAudience = true,
                ValidAudiences = _config["Jwt:Audience"]?.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(a => a.Trim()),

                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!)),

                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            ClaimsPrincipal? principal;
            SecurityToken? validatedAccess;
            bool accessExpired = false;

            try
            {
                principal = _handler.ValidateToken(accessToken, tokenParams, out validatedAccess);

                // token is still valid + not expiring soon...
                var exp = DateTimeOffset.FromUnixTimeSeconds(((JwtSecurityToken)validatedAccess).Payload.Expiration!.Value);
                if (exp - DateTimeOffset.UtcNow > TimeSpan.FromMinutes(5))
                {
                    _logger.LogInformation("Access token is valid and not expiring soon for user: {Username}", username);
                    //return new TokenValidation(true, Principal: principal);
                    return new(true, Principal: principal);
                }
                else
                {
                    _logger.LogWarning("Access token is valid but expiring soon for user: {Username}. Attempting refresh.", username);
                    accessExpired = true;
                }
            }
            catch (SecurityTokenExpiredException)
            {
                _logger.LogWarning("Access token expired for user: {Username}. Attempting refresh.", username);
                accessExpired = true; // Mark for refresh
                principal = null; // Principal from expired token is not valid
            }
            catch (SecurityTokenValidationException ex)
            {
                _logger.LogError(ex, "Access token validation failed for user: {Username} (Reason: {Message})", username, ex.Message);
                //return new TokenValidation(false, "Invalid access token."); // Token invalid for other reasons, cannot refresh
                return new(false, "Invalid access token.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error validating access token for user: {Username}", username);
                //return new TokenValidation(false, "An unexpected error occurred during access token validation.");
                return new(false, "An unexpected error occurred during access token validation.");
            }

            if (accessExpired && tryRefresh)
            {
                try
                {
                    var refreshTokenParams = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = _config["Jwt:Issuer"],
                        ValidateAudience = true,
                        ValidAudiences = _config["Jwt:Audience"]?.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(a => a.Trim()),
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!)),
                        ValidateLifetime = true, // Ensure refresh token itself is not expired
                        ClockSkew = TimeSpan.Zero
                    };

                    ClaimsPrincipal refreshPrincipal = _handler.ValidateToken(refreshToken, refreshTokenParams, out var validatedRefreshToken);
                    var refreshTokenExp = DateTimeOffset.FromUnixTimeSeconds(((JwtSecurityToken)validatedRefreshToken).Payload.Expiration!.Value);

                    if (refreshTokenExp <= DateTimeOffset.UtcNow)
                    {
                        _logger.LogWarning("Refresh token expired for user: {Username}. Login required.", username);
                        //return new TokenValidation(false, "Refresh token has expired. Please log in again.");
                        return new(false, "Refresh token has expired. Please log in again.");
                    }

                    // refresh token is valid, generate new tokens...
                    _logger.LogInformation("Refresh token is valid for user: {Username}. Generating new tokens.", username);
                    var (newAccess, newRefresh) = GenerateToken(username);

                    // re-validate the new access token to get the principal for the response...
                    var newPrincipal = _handler.ValidateToken(newAccess, tokenParams, out var _);

                    //return new TokenValidation(true, Principal: newPrincipal, AccessToken: newAccess, RefreshToken: newRefresh);
                    return new(true, Principal: newPrincipal, AccessToken: newAccess, RefreshToken: newRefresh);
                }
                catch (SecurityTokenExpiredException)
                {
                    _logger.LogWarning("Refresh token expired for user: {Username}. Login required.", username);
                    //return new TokenValidation(false, "Refresh token has expired. Please log in again.");
                    return new(false, "Refresh token has expired. Please log in again.");
                }
                catch (SecurityTokenValidationException ex)
                {
                    _logger.LogError(ex, "Refresh token validation failed for user: {Username} (Reason: {Message})", username, ex.Message);
                    //return new TokenValidation(false, "Invalid refresh token. Please log in again.");
                    return new(false, "Invalid refresh token. Please log in again.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error validating refresh token for user: {Username}", username);
                    //return new TokenValidation(false, "An unexpected error occurred during refresh token validation.");
                    return new(false, "An unexpected error occurred during refresh token validation.");
                }
            }

            else if (accessExpired && !tryRefresh)
            {
                _logger.LogInformation("Access token expired for user: {Username}, but refresh is disabled.", username);
                //return new TokenValidation(false, "Access token is expired and refreshing is disabled. Start a new session.");
                return new(false, "Access token is expired and refreshing is disabled. Start a new session.");
            }
            else // This branch covers cases where access token was invalid for non-expiry reasons, or refresh was not attempted.
            {
                // This state should ideally be handled by the initial `catch (SecurityTokenValidationException ex)` for access token.
                // Reaching here might mean the access token was valid but just "near expiry" and `tryRefresh` was false,
                // or some other unforeseen path. For safety, return false if no valid path was found.
                _logger.LogInformation("No token refresh performed or access token was already invalid for user: {Username}", username);
                //return new TokenValidation(false, "Invalid or insufficient tokens to establish a session.");
                return new(false, "Invalid or insufficient tokens to establish a session.");
            }
        }

        public (bool success, string message) AuthorizeRequest(HttpContext context)
        {
            var request = context.Request;
            var response = context.Response;

            // fetch username from cookies before interacting with tokens...
            var username = request.Cookies["username"];
            if (string.IsNullOrEmpty(username))
            {
                _logger.LogInformation("Authorization failed: Username cookie missing.");
                return (false, "Username is missing");
            }

            // fetch tokens from cookies to validate session...
            var accessToken = request.Cookies["access_token"];
            var refreshToken = request.Cookies["refresh_token"];
            if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(refreshToken))
            {
                _logger.LogInformation("Authorization failed: Access or refresh token cookie missing for user: {Username}", username);
                return (false, "Access token is missing");
            }

            // validate tokens + refresh if allowed...
            //var tokenService = new TokenService(_config);
            var result = ValidateTokens(accessToken, refreshToken, username);
            if (!result.IsValid)
            {
                _logger.LogWarning("Authorization failed for user: {Username}. Reason: {Message}", username, result.Message);
                return (false, "Invalid access token, authorization failed.");
            }

            // if non-null, replace tokens in cookies with fresh set...
            if (result.AccessToken != null && result.RefreshToken != null)
            {
                var cookies = request.Cookies.ToList();
                foreach (var cookie in cookies)
                {
                    switch (cookie.Key.ToLowerInvariant())
                    {
                        case "access_token":
                            response.Cookies.Append("access_token", result.AccessToken, _cookieService.AccessOptions());
                            break;
                        case "refresh_token":
                            response.Cookies.Append("refresh_token", result.RefreshToken, _cookieService.RefreshOptions());
                            break;
                        default:
                            response.Cookies.Append(cookie.Key, cookie.Value, _cookieService.RefreshOptions());
                            break;
                    }
                }
            }

            context.User = result.Principal!; // Principal should always be non-null if IsValid is true
            _logger.LogInformation("Authorization granted for user: {Username}", username);
            return (true, "Token has been validated, authorization granted.");
        }
    }
}