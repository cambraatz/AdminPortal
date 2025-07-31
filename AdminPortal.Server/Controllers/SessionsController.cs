using AdminPortal.Server.Models;
using AdminPortal.Server.Services;
using AdminPortal.Server.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;

namespace AdminPortal.Server.Controllers
{
    [ApiController]
    [Route("v1/sessions")]
    public class SessionsController : Controller
    {
        private readonly IUserService _userService;
        private readonly ITokenService _tokenService;
        private readonly ICookieService _cookieService;
        private readonly IMappingService _mappingService;
        private readonly ISessionService _sessionService;
        private readonly ILogger<SessionsController> _logger;
        private readonly IWebHostEnvironment _env;

        public SessionsController(
            IUserService userService,
            ITokenService tokenService,
            ICookieService cookieService, 
            IMappingService mappingService,
            ISessionService sessionService,
            ILogger<SessionsController> logger,
            IWebHostEnvironment env)
        {
            _userService = userService;
            _tokenService = tokenService;
            _cookieService = cookieService;
            _mappingService = mappingService;
            _sessionService = sessionService;
            _logger = logger;
            _env = env;
        }

        // simulates passed data from login app/service...
        [HttpGet]
        [Route("dev-login")]
        public async Task<IActionResult> DevLogin(
            [FromQuery] string? username = "cbraatz",
            [FromQuery] string? company = "BRAUNS",
            long userId = 0)
        {
            // ensure development environment only...
            if (!_env.IsDevelopment())
            {
                _logger.LogWarning("Attempted to access DevLogin endpoint in non-development environment.");
                return NotFound("This endpoint is only available in the Development environment.");
            }

            // ensure valid username and company parameters...
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(company))
            {
                return BadRequest(new { message = "Username and company are both required for dev login." });
            }

            // generate valid access/refresh tokens for dev session...
            (string access, string refresh) = await _tokenService.GenerateToken(username, userId);

            // fetch user credentials from local DB...
            UserCredentials? user = await _userService.GetByUsernameAsync(username);
            if (user == null)
            {
                _logger.LogWarning("Development user '{Username}' not found in local DB", username);
                return NotFound(new { message = $"Development user '{username}' not found in local database. User must be created prior to login." });
            }

            // fetch company and module mappings...
            IDictionary<string, string> companies = await _mappingService.GetCompaniesAsync();
            IDictionary<string, string> modules = await _mappingService.GetModulesAsync();

            /* add max size warning optional */
            Response.Cookies.Append("username", user.Username!, _cookieService.AccessOptions());
            Response.Cookies.Append("company", company, _cookieService.AccessOptions());
            Response.Cookies.Append("access_token", access, _cookieService.AccessOptions());
            Response.Cookies.Append("refresh_token", refresh, _cookieService.RefreshOptions());

            Response.Cookies.Append("company_mapping", JsonSerializer.Serialize(companies), _cookieService.AccessOptions());
            Response.Cookies.Append("module_mapping", JsonSerializer.Serialize(modules), _cookieService.AccessOptions());

            return Redirect("https://localhost:5173/");
        }

        // validates existing user session...
        [HttpGet]
        [Route("me")]
        [Authorize]
        public async Task<IActionResult> GetCurrentDriver()
        {
            // ensure valid cookies...
            string? username = Request.Cookies["username"];
            if (string.IsNullOrEmpty(username))
            {
                _logger.LogWarning("Required 'username' cookie is missing or empty.");
                return BadRequest(new { message = "Username cookies is missing or empty." });
            }

            string? companyMapping = Request.Cookies["company_mapping"];
            if (string.IsNullOrEmpty(companyMapping))
            {
                _logger.LogWarning("Required 'company_mapping' cookie is missing or empty.");
                return BadRequest(new { message = "Company mapping cookie is missing or empty." });
            }

            string? moduleMapping = Request.Cookies["module_mapping"];
            if (string.IsNullOrEmpty(moduleMapping))
            {
                _logger.LogWarning("Required 'module_mapping' cookie is missing or empty.");
                return BadRequest(new { message = "Module mapping cookie is missing or empty." });
            }

            string? accessToken = Request.Cookies["access_token"];
            string? refreshToken = Request.Cookies["refresh_token"];
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(refreshToken))
            {
                _logger.LogWarning("me: Missing required token cookies for user {Username}.", username);
                return Unauthorized(new { message = "Session token cookies are missing. Please log in again." });
            }

            // fetch existing user by username...
            UserCredentials? user = await _userService.GetByUsernameAsync(username); /* DANGER: DO NOT PASS PASSWORD */
            if (user == null)
            {
                return NotFound(new { message = "Driver not found." });
            }

            // initialize send-safe user...
            User sendSafeUser = new User()
            {
                Username = user.Username,
                Permissions = null,
                Powerunit = user.Powerunit,
                ActiveCompany = user.Companies?.First(),
                Companies = user.Companies,
                Modules = user.Modules
            };

            SessionModel? session = await _sessionService.GetSessionAsync(username, accessToken, refreshToken);
            if (session != null)
            {
                return Ok(new { user = user, companies = companyMapping, modules = moduleMapping, userId = session.Id });
            }

            return Unauthorized(new { message = "Session cookies are missing. Please log in again." });
        }

        // terminates existing user session by scrubbing cookies...
        /*[HttpPost]
        [Route("logout")]
        public IActionResult Logout()
        {
            // set all cookies to expire...
            foreach (var cookie in Request.Cookies)
            {
                Response.Cookies.Append(cookie.Key, "", _cookieService.RemoveOptions());
            }
            return Ok(new { message = "Logged out successfully" });
        }*/

        [HttpPost]
        [Route("logout/{userId}")]
        public async Task<IActionResult> Logout([FromBody] SessionModel? session, long userId)
        {
            if (session != null)
            {
                // Always attempt to get tokens from cookies, as they are the primary source for browser sessions
                string? accessTokenFromRequest = Request.Cookies["access_token"];
                string? refreshTokenFromRequest = Request.Cookies["refresh_token"];

                // If the SessionModel was provided (e.g., from a request body),
                // and it contains a username, use it. Otherwise, get it from what's available.
                string? username = session?.Username;
                string? powerUnit = session?.PowerUnit;
                string? mfstDate = session?.MfstDate;

                // Prioritize tokens from cookies. If not found, use from the session model if present.
                // Ensure we coalesce to string.Empty if ultimately null, as service methods might expect string.
                string accessToken = accessTokenFromRequest
                                            ?? session?.AccessToken // Fallback to session model's token
                                            ?? string.Empty;       // Default to empty string if all are null

                string refreshToken = refreshTokenFromRequest
                                            ?? session?.RefreshToken // Fallback to session model's token
                                            ?? string.Empty;

                bool sessionCleared = false;

                // delete session by ID...
                _logger.LogWarning($"Invalidate by session ID {userId}");
                sessionCleared = await _sessionService.DeleteUserSessionByIdAsync(userId);

                // stale session, clean expired sessions...
                if (!sessionCleared)
                {
                    _logger.LogWarning("Failed to clear session for user {Username} during logout.", username);
                    await _sessionService.CleanupExpiredSessionsAsync(TimeSpan.FromMinutes(30));
                }
                else
                {
                    _logger.LogWarning("Successfully cleared session for user {Username} during logout.", username);
                }
            }
            // remove all cookies and return...
            foreach (var cookie in Request.Cookies)
            {
                Response.Cookies.Append(cookie.Key, "", _cookieService.RemoveOptions());
            }
            return Ok(new { message = "Logged out successfully" });
        }

        // initializes a valid return session...
        /*[HttpPost]
        [Route("return")]
        [Authorize]
        public IActionResult Return()
        {
            // refresh active cookies and add valid return cookie...
            _cookieService.ExtendCookies(HttpContext, 15);
            Response.Cookies.Append("return", "true", _cookieService.AccessOptions());

            return Ok(new { message = "Returning, cookies extension completed successfully." });
        }*/

        [HttpPost]
        [Route("return/{userId}")]
        [Authorize]
        public async Task<IActionResult> Return(long userId)
        {
            string? username = Request.Cookies["username"];
            string? accessToken = Request.Cookies["access_token"];
            string? refreshToken = Request.Cookies["refresh_token"];
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(refreshToken))
            {
                _logger.LogWarning("Return: Missing required cookies for user {Username}.", username);
                return Unauthorized(new { message = "Session cookies are missing. Please log in again." });
            }

            /*bool sessionCleared = await _sessionService.InvalidateSessionAsync(username, accessToken, refreshToken);
            if (!sessionCleared)
            {
                _logger.LogWarning("Return: Failed to clear session for user {Username}.", username);
                return StatusCode(500, "Failed to clear session. Please try again later.");
            }*/
            DateTime refreshExpiryTime = DateTime.UtcNow.AddDays(1);
            try
            {
                var refreshJwtToken = new JwtSecurityTokenHandler().ReadJwtToken(refreshToken);
                if (refreshJwtToken.Payload.Expiration.HasValue)
                {
                    refreshExpiryTime = DateTimeOffset.FromUnixTimeSeconds(refreshJwtToken.Payload.Expiration.Value).UtcDateTime;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CheckManifestAccess: Could not parse refresh token expiry for user {Username}. Using default expiry.", username);
            }

            SessionModel? session = await _sessionService.AddOrUpdateSessionAsync(
                userId,
                username,
                accessToken,
                refreshToken,
                refreshExpiryTime,
                null,
                null
            );

            if (session != null)
            {
                _cookieService.ExtendCookies(HttpContext, 15);
                Response.Cookies.Append("return", "true", _cookieService.AccessOptions());

                return Ok(new { message = "Returning, cookies extension completed successfully." });
            }

            _logger.LogError($"CheckManifestAccess: Failed to release session access for user {username}.");
            return StatusCode(500, "Failed to release session with manifest details.");
        }
    }
}
