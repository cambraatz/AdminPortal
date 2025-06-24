using Microsoft.Extensions.Options;
using AdminPortal.Server.Services.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.Net;

namespace AdminPortal.Server.Services
{
    public class CookieService : ICookieService
    {
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _config;
        private readonly ILogger<CookieService> _logger;

        public CookieService(IWebHostEnvironment env, IConfiguration config, ILogger<CookieService> logger)
        {
            _env = env;
            _config = config;
            _logger = logger;
        }

        // HELPER: generate and log results of env query, dynamically returns cookie domain...
        private string? GetCookieDomain()
        {
            if (_env.IsDevelopment())
            {
                // let domain be set to current origins domain (likely localhost)
                _logger.LogInformation("CookieService: Running in Development environment, setting cookie domain to null (auto-localhost).");
                Console.WriteLine("CookieService: Running in Development environment, setting cookie domain to null (auto-localhost).");
                return null;
            }
            // explicitly set origin domain to deployment URL
            _logger.LogInformation("CookieService: Running in non-Development environment, setting cookie domain to .tcsservices.com.");
            return ".tcsservices.com";
        }

        // packaged remove/delete cookie parameters (ie: set to expired)...
        public CookieOptions RemoveOptions()
        {
            return new CookieOptions
            {
                Expires = DateTime.UtcNow.AddDays(-1),
                HttpOnly = true,
                Secure = true,
                Domain = GetCookieDomain(),
                SameSite = SameSiteMode.None,
                Path = "/"
            };
        }

        // packaged access cookie parameters (ie: extend 15 mins)...
        public CookieOptions AccessOptions()
        {
            return new CookieOptions
            {

                Expires = DateTime.UtcNow.AddMinutes(15),
                HttpOnly = true,
                Secure = true,
                Domain = GetCookieDomain(),
                SameSite = SameSiteMode.None,
                Path = "/"
            };
        }

        // packaged refresh cookie parameters (ie: extend 1 day)...
        public CookieOptions RefreshOptions()
        {
            return new CookieOptions
            {
                Expires = DateTime.UtcNow.AddDays(1),
                HttpOnly = true,
                Secure = true,
                Domain = GetCookieDomain(),
                SameSite = SameSiteMode.None,
                Path = "/"
            };
        }

        // refresh session cookies to refresh by default...
        public void ExtendCookies(HttpContext context, int extensionMinutes)
        {
            var response = context.Response;
            var request = context.Request;

            foreach (var cookie in request.Cookies)
            {
                switch (cookie.Key.ToLowerInvariant())
                {
                    case "access_token":
                        response.Cookies.Append("access_token", cookie.Value, AccessOptions());
                        break;
                    default:
                        response.Cookies.Append(cookie.Key, cookie.Value, RefreshOptions());
                        break;
                }
            }
        }
    }
}
