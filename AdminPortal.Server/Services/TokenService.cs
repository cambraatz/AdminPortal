using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace AdminPortal.Server.Services
{
    public class TokenService
    {
        private readonly IConfiguration _configuration;
        public TokenService(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public (string AccessToken, string RefreshToken) GenerateToken(string username, string company = null)
        {
            var accessClaims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

            // add company if present...
            if (!string.IsNullOrEmpty(company))
            {
                accessClaims.Add(new Claim("Company", company));
            }

            var refreshClaims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var accessToken = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: accessClaims,
                expires: DateTime.UtcNow.AddMinutes(15),
                signingCredentials: creds);

            var refreshToken = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: refreshClaims,
                expires: DateTime.UtcNow.AddDays(1),
                signingCredentials: creds);

            return (new JwtSecurityTokenHandler().WriteToken(accessToken), new JwtSecurityTokenHandler().WriteToken(refreshToken));
        }

        // Helper method to validate the access token
        public class TokenValidation
        {
            public bool IsValid { get; set; }
            public string? Message { get; set; }
            public ClaimsPrincipal? Principal { get; set; }
            public string? accessToken { get; set; }
            public string? refreshToken { get; set; }
        }

        public TokenValidation ValidateAccessToken(string accessToken, string username)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            try
            {
                var principal = tokenHandler.ValidateToken(accessToken, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"])),
                    ValidateIssuer = true,
                    ValidIssuer = _configuration["Jwt:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = _configuration["Jwt:Audience"],
                    ValidateLifetime = true,  // ensure token is not expired
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                var jwtToken = validatedToken as JwtSecurityToken;
                var exp = jwtToken?.Payload.Exp ?? 0;
                var expirationTime = DateTimeOffset.FromUnixTimeSeconds(exp);
                var currentTime = DateTimeOffset.UtcNow;



                // check if token is close to expiring
                if (expirationTime - currentTime < TimeSpan.FromMinutes(5))
                {
                    var newTokens = GenerateToken(username);
                    return new TokenValidation { IsValid = true, Principal = principal, accessToken = newTokens.AccessToken, refreshToken = newTokens.RefreshToken };
                }

                return new TokenValidation { IsValid = true, Principal = principal };
            }
            catch (Exception ex)
            {
                return new TokenValidation { IsValid = false, Message = ex.Message };
            }
        }
    }
}
