/*/////////////////////////////////////////////////////////////////////////////

Author: Cameron Braatz
Date: 3/12/2025
Update: --/--/----

*//////////////////////////////////////////////////////////////////////////////

using AdminPortal.Server.Models;
using AdminPortal.Server.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Data;
using System.Data.SqlClient;
using System;


// token initialization...
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Threading.Tasks;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using static AdminPortal.Server.Services.TokenService;

/*public class TokenService
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
}*/

public class RefreshRequest
{
    public string? Username { get; set; }
    public string? RefreshToken { get; set; }
}

public class CompanyRequest
{
    public string? Company { get; set; }
    public string? AccessToken { get; set; }
}

namespace AdminPortal.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IHostEnvironment _environment;
        private readonly ILogger<AdminController> _logger;
        //private readonly TokenService _tokenService;
        private readonly string connString;

        public AdminController(IConfiguration configuration, TokenService tokenService)
        {
            _configuration = configuration;
            //_tokenService = tokenService;
            //connString = _configuration.GetConnectionString("DriverChecklistTestCon");
            //connString = _configuration.GetConnectionString("LOCAL");
            connString = _configuration.GetConnectionString("TCSWEB");
        }

        [HttpPost]
        [Route("RefreshToken")]
        public IActionResult RefreshToken([FromBody] RefreshRequest request)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(request.RefreshToken, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"])),
                ValidateIssuer = true,
                ValidIssuer = _configuration["Jwt:Issuer"],
                ValidateAudience = true,
                ValidAudience = _configuration["Jwt:Audience"],
                ValidateLifetime = false
            }, out SecurityToken validatedToken);

            if (validatedToken is JwtSecurityToken jwtToken &&
                jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                var tokenService = new TokenService(_configuration);
                var tokens = tokenService.GenerateToken(request.Username);
                return Ok(new { AccessToken = tokens.AccessToken, RefreshToken = tokens.RefreshToken });
            }
            return Unauthorized("Invalid Token.");
        }

        [HttpPost]
        [Route("Logout")]
        public IActionResult Logout()
        {
            CookieOptions options = new CookieOptions
            {
                Expires = DateTime.UtcNow.AddDays(-1),
                HttpOnly = true,
                Secure = true,
                Domain = ".tcsservices.com",
                SameSite = SameSiteMode.None,
                Path = "/"
            };

            /*Response.Cookies.Append("access_token", "", options);
            Response.Cookies.Append("refresh_token", "", options);
            Response.Cookies.Append("username", "", options);
            Response.Cookies.Append("company", "", options);*/

            foreach (var cookie in Request.Cookies)
            {
                Response.Cookies.Append(cookie.Key, cookie.Value, options);
            };

            return Ok(new { message = "Logged out successfully" });
        }

        [HttpPost]
        [Route("Return")]
        public IActionResult Return()
        {
            CookieService.ExtendCookies(HttpContext, 15);

            CookieOptions options = new CookieOptions
            {
                Expires = DateTime.UtcNow.AddMinutes(15),
                HttpOnly = true,
                Secure = true,
                Domain = ".tcsservices.com",
                SameSite = SameSiteMode.None,
                Path = "/"
            };

            Response.Cookies.Append("return", "true", options);

            return Ok(new { message = "Returning, cookies extended by 15 minutes." });
        }

        [HttpPost]
        [Route("ValidateUser")]
        public async Task<JsonResult> ValidateUser()
        {
            var accessToken = Request.Cookies["access_token"];
            var refreshToken = Request.Cookies["refresh_token"];
            var username = Request.Cookies["username"];

            var tokenService = new TokenService(_configuration);

            if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(refreshToken))
            {
                return new JsonResult(new { success = false, message = "Access token is missing" });
            }
            if (string.IsNullOrEmpty(username))
            {
                return new JsonResult(new { success = false, message = "Username is missing" });
            }

            //var tokenService = new TokenService(_configuration);
            TokenValidation result = tokenService.ValidateAccessToken(accessToken,username);
            if (!result.IsValid)
            {
                return new JsonResult(new { success = false, message = "Invalid or expired access token" });
            }

            if (result.accessToken != null && result.refreshToken != null)
            {
                CookieOptions options = new CookieOptions
                {
                    Expires = DateTime.UtcNow.AddMinutes(15),
                    HttpOnly = true,
                    Secure = true,
                    Domain = ".tcsservices.com",
                    SameSite = SameSiteMode.None,
                    Path = "/"
                };

                Response.Cookies.Append("access_token", result.accessToken, options);

                options.Expires = DateTime.UtcNow.AddDays(1);

                Response.Cookies.Append("refresh_token", result.refreshToken, options);
            }

            string query = "select * from dbo.USERS where USERNAME COLLATE SQL_Latin1_General_CP1_CS_AS = @USERNAME";
            DataTable table = new DataTable();

            string sqlDatasource = connString;

            await using (var myCon = new SqlConnection(sqlDatasource))
            {
                await myCon.OpenAsync();
                using (var myCommand = new SqlCommand(query, myCon))
                {
                    myCommand.Parameters.AddWithValue("@USERNAME", username);

                    using (var myReader = await myCommand.ExecuteReaderAsync())
                    {
                        table.Load(myReader);
                    }
                }
            }
            if (table.Rows.Count > 0)
            {
                User user = new User
                {
                    Username = username,
                    Permissions = table.Rows[0]["PERMISSIONS"] != DBNull.Value ? table.Rows[0]["PERMISSIONS"].ToString() : null,
                    Powerunit = table.Rows[0]["POWERUNIT"].ToString(),
                    ActiveCompany = table.Rows[0]["COMPANYKEY01"].ToString(),
                    Companies = new List<string>(),
                    Modules = new List<string>()
                };

                return new JsonResult(new { success = true, user = user, accessToken = accessToken, refreshToken = refreshToken });
            }
            else
            {
                return new JsonResult(new { success = false, message = "Invalid Credentials" });
            }
        }

        // queries TCSWEB dbo.USERS...
        [HttpPut]
        [Route("AddDriver")]
        public async Task<JsonResult> AddDriver([FromBody] User newUser)
        {
            /*
            var company = Request.Cookies["company"];
            if (string.IsNullOrEmpty(company))
            {
                return new JsonResult(new { success = false, message = "Company key is missing." });
            }

            string sqlDatasource = _configuration.GetConnectionString(company);
            */

            string insertQuery = "INSERT INTO dbo.USERS(USERNAME, PASSWORD, POWERUNIT, COMPANYKEY01, COMPANYKEY02, COMPANYKEY03, COMPANYKEY04, COMPANYKEY05, " +
                                    "MODULE01, MODULE02, MODULE03, MODULE04, MODULE05, MODULE06, MODULE07, MODULE08, MODULE09, MODULE10) " +
                                    "VALUES (@USERNAME, @PASSWORD, @POWERUNIT, @COMPANYKEY01, @COMPANYKEY02, @COMPANYKEY03, @COMPANYKEY04, @COMPANYKEY05, " +
                                    "@MODULE01, @MODULE02, @MODULE03, @MODULE04, @MODULE05, @MODULE06, @MODULE07, @MODULE08, @MODULE09, @MODULE10)";
            string selectQuery = "SELECT * FROM dbo.USERS";

            DataTable table = new DataTable();
            string sqlDatasource = connString;

            await using (SqlConnection myCon = new SqlConnection(sqlDatasource))
            {
                try
                {
                    // open the db connection...
                    myCon.Open();

                    // insert new user to dbo.USERS...
                    using (SqlCommand insertCommand = new SqlCommand(insertQuery, myCon))
                    {
                        insertCommand.Parameters.AddWithValue("@USERNAME", newUser.Username);
                        insertCommand.Parameters.AddWithValue("@PASSWORD", DBNull.Value);
                        insertCommand.Parameters.AddWithValue("@POWERUNIT", newUser.Powerunit);

                        for (int i = 0; i < 5; i++)
                        {
                            if (i < newUser.Companies.Count && newUser.Companies[i] != null)
                            {
                                insertCommand.Parameters.AddWithValue($"@COMPANYKEY0{i + 1}", newUser.Companies[i]);
                            } else
                            {
                                insertCommand.Parameters.AddWithValue($"@COMPANYKEY0{i + 1}", DBNull.Value);
                            }
                        }

                        for (int i = 0; i < 10; i++)
                        {
                            string index = i+1 < 10 ? $"0{i+1}" : (i+1).ToString();

                                if (i < newUser.Modules.Count && newUser.Modules[i] != null)
                                {
                                    insertCommand.Parameters.AddWithValue($"@MODULE{index}", newUser.Modules[i]);
                                } 
                                else
                                {
                                    insertCommand.Parameters.AddWithValue($"@MODULE{index}", DBNull.Value);
                                }
                        }

                        int insertResponse = await insertCommand.ExecuteNonQueryAsync();

                        // check if new user was inserted successfully...
                        if (insertResponse <= 0)
                        {
                            return new JsonResult("Error creating new user.");
                        }
                    }

                    // gather the new table contents for dbo.USERS...
                    using (SqlCommand selectCommand = new SqlCommand(selectQuery, myCon))
                    {
                        SqlDataAdapter adapter = new SqlDataAdapter(selectCommand);
                        adapter.Fill(table);
                    }

                    // close db connection...
                    myCon.Close();

                    // return success message...
                    return new JsonResult(new { success = true, table = table });
                }
                catch (Exception ex)
                {
                    //return new JsonResult("Error: " + ex.Message);
                    return new JsonResult(new { success = false, error = ex.Message });
                }
            }
        }

        // queries TCSWEB dbo.USERS...
        [HttpPost]
        [Route("PullDriver")]
        public async Task<JsonResult> PullDriver([FromBody] driverRequest request)
        {
            //string query = "SELECT USERNAME, PASSWORD, POWERUNIT FROM dbo.USERS WHERE USERNAME = @USERNAME";
            string query = "SELECT * FROM dbo.USERS WHERE USERNAME = @USERNAME";

            DataTable table = new DataTable();
            string sqlDatasource = connString;
            driverCredentials driver = new driverCredentials();

            await using (SqlConnection myCon = new SqlConnection(sqlDatasource))
            {
                try
                {
                    myCon.Open();
                    using (SqlCommand myCommand = new SqlCommand(query, myCon))
                    {
                        myCommand.Parameters.AddWithValue("@USERNAME", request.USERNAME);
                        using (SqlDataReader myReader = await myCommand.ExecuteReaderAsync())
                        {
                            if (myReader.Read())
                            {
                                driver.USERNAME = myReader["USERNAME"].ToString();
                                driver.PASSWORD = myReader["PASSWORD"].ToString();
                                driver.POWERUNIT = myReader["POWERUNIT"].ToString();

                                List<string> companies = new List<string>();
                                List<string> modules = new List<string>();

                                for (int i = 1; i <= 10; i++)
                                {
                                    string index = i < 10 ? $"0{i}" : (i).ToString();
                                    if (i <= 5)
                                    {
                                        companies.Add(myReader[$"COMPANYKEY{index}"] as string);
                                    }

                                    modules.Add(myReader[$"MODULE{index}"] as string);
                                }

                                driver.COMPANIES = companies;
                                driver.MODULES = modules;
                            }
                            myReader.Close();
                        }
                    }
                    myCon.Close();

                    // generate token...
                    var tokenService = new TokenService(_configuration);
                    (string accessToken, string refreshToken) = tokenService.GenerateToken(driver.USERNAME);

                    // validate password...
                    bool valid = driver.PASSWORD == null || driver.PASSWORD == "" ? false : true;
                    return new JsonResult(new
                    {
                        success = true,
                        username = driver.USERNAME,
                        password = driver.PASSWORD,
                        powerunit = driver.POWERUNIT,
                        user = driver,
                        accessToken = accessToken,
                        refreshToken = refreshToken
                    });
                }
                catch (Exception ex)
                {
                    return new JsonResult(new { success = false, error = ex.Message });
                }
            }
        }

        // queries TCSWEB dbo.USERS...
        [HttpPut]
        [Route("ReplaceDriver")]
        [Authorize]
        public async Task<JsonResult> ReplaceDriver([FromBody] driverReplacement driver)
        {
            string deleteQuery = "DELETE FROM dbo.USERS WHERE USERNAME = @PREVUSER";
            //string insertQuery = "INSERT INTO dbo.USERS(USERNAME, PASSWORD, POWERUNIT) VALUES (@USERNAME, @PASSWORD, @POWERUNIT)";
            string insertQuery = "INSERT INTO dbo.USERS(USERNAME, PASSWORD, POWERUNIT, COMPANYKEY01, COMPANYKEY02, COMPANYKEY03, COMPANYKEY04, COMPANYKEY05, " +
                                    "MODULE01, MODULE02, MODULE03, MODULE04, MODULE05, MODULE06, MODULE07, MODULE08, MODULE09, MODULE10) " +
                                    "VALUES (@USERNAME, @PASSWORD, @POWERUNIT, @COMPANYKEY01, @COMPANYKEY02, @COMPANYKEY03, @COMPANYKEY04, @COMPANYKEY05, " +
                                    "@MODULE01, @MODULE02, @MODULE03, @MODULE04, @MODULE05, @MODULE06, @MODULE07, @MODULE08, @MODULE09, @MODULE10)";

            string selectQuery = "SELECT * FROM dbo.USERS";

            DataTable table = new DataTable();
            string sqlDatasource = connString;

            await using (SqlConnection myCon = new SqlConnection(sqlDatasource))
            {
                try
                {
                    // open the db connection...
                    myCon.Open();

                    // delete the old user from dbo.USERS...
                    using (SqlCommand deleteCommand = new SqlCommand(deleteQuery, myCon))
                    {
                        deleteCommand.Parameters.AddWithValue("@PREVUSER", driver.PrevUser);

                        int deleteResult = await deleteCommand.ExecuteNonQueryAsync();

                        // check if old user was deleted successfully...
                        if (deleteResult <= 0)
                        {
                            return new JsonResult("Error deleting previous user, moving forward with replacing user.");
                        }
                    }

                    // insert new user to dbo.USERS...
                    using (SqlCommand insertCommand = new SqlCommand(insertQuery, myCon))
                    {
                        insertCommand.Parameters.AddWithValue("@USERNAME", driver.Username);
                        insertCommand.Parameters.AddWithValue("@PASSWORD", string.IsNullOrEmpty(driver.Password) ? DBNull.Value : driver.Password);
                        insertCommand.Parameters.AddWithValue("@POWERUNIT", driver.Powerunit);

                        for (int i = 0; i < 5; i++)
                        {
                            if (i < driver.Companies.Count && driver.Companies[i] != null)
                            {
                                insertCommand.Parameters.AddWithValue($"@COMPANYKEY0{i + 1}", driver.Companies[i]);
                            }
                            else
                            {
                                insertCommand.Parameters.AddWithValue($"@COMPANYKEY0{i + 1}", DBNull.Value);
                            }
                        }

                        for (int i = 0; i < 10; i++)
                        {
                            string index = i + 1 < 10 ? $"0{i + 1}" : (i + 1).ToString();

                            if (i < driver.Modules.Count && driver.Modules[i] != null)
                            {
                                insertCommand.Parameters.AddWithValue($"@MODULE{index}", driver.Modules[i]);
                            }
                            else
                            {
                                insertCommand.Parameters.AddWithValue($"@MODULE{index}", DBNull.Value);
                            }
                        }

                        int insertResponse = await insertCommand.ExecuteNonQueryAsync();

                        // check if new user was inserted successfully...
                        if (insertResponse <= 0)
                        {
                            return new JsonResult("Error creating new user.");
                        }
                    }

                    // gather the new table contents for dbo.USERS...
                    using (SqlCommand selectCommand = new SqlCommand(selectQuery, myCon))
                    {
                        SqlDataAdapter adapter = new SqlDataAdapter(selectCommand);
                        adapter.Fill(table);
                    }

                    // close db connection...
                    myCon.Close();
                    
                    var user = Request.Cookies["username"];
                    if (string.IsNullOrEmpty(user))
                    {
                        return new JsonResult(new { success = false, value = "Retrieving username from cookies failed." });
                    }

                    if (user == driver.PrevUser)
                    {
                        Response.Cookies.Append("username", driver.Username, new CookieOptions
                        {
                            HttpOnly = true, // Makes it inaccessible to JavaScript
                            Secure = true, // Ensures the cookie is only sent over HTTPS
                            SameSite = SameSiteMode.None, // Allows sharing across subdomains
                            Domain = ".tcsservices.com", // Cookie available for all subdomains of domain.com
                            Expires = DateTimeOffset.UtcNow.AddMinutes(15),
                            Path = "/"
                        });

                        // return success message...
                        return new JsonResult(new { success = true, table = table, message = $"Current username {driver.Username} has been successfully edited." });
                    }
                    // return success message...
                    return new JsonResult(new { success = true, table = table, message = $"User {driver.Username} has been successfully edited." });
                }
                catch (Exception ex)
                {
                    return new JsonResult("Error: " + ex.Message);
                }
            }
        }

        // queries TCSWEB dbo.USERS...
        [HttpDelete]
        [Route("DeleteDriver")]
        //[Authorize]
        public JsonResult DeleteDriver(string USERNAME)
        {
            string query = "delete from dbo.USERS where USERNAME=@USERNAME";
            string selectQuery = "SELECT * FROM dbo.USERS";

            DataTable table = new DataTable();
            string sqlDatasource = connString;
            SqlDataReader myReader;

            using (SqlConnection myCon = new SqlConnection(sqlDatasource))
            {
                try
                {
                    myCon.Open();
                    using (SqlCommand myCommand = new SqlCommand(query, myCon))
                    {

                        myCommand.Parameters.AddWithValue("@USERNAME", USERNAME);
                        myReader = myCommand.ExecuteReader();
                        table.Load(myReader);
                        myReader.Close();

                    }

                    // gather the new table contents for dbo.USERS...
                    using (SqlCommand selectCommand = new SqlCommand(selectQuery, myCon))
                    {
                        SqlDataAdapter adapter = new SqlDataAdapter(selectCommand);
                        adapter.Fill(table);
                    }
                    myCon.Close();

                    // return success message...
                    return new JsonResult(new { success = true });
                }
                catch (Exception ex)
                {
                    return new JsonResult(new { success = false, error = "Error: " + ex.Message });

                }
            }
        }

        [HttpGet]
        [Route("CollectModules")]
        public JsonResult CollectModules()
        {
            string query = "select * from dbo.MODULE";

            DataTable table = new DataTable();
            string sqlDatasource = connString;
            SqlDataReader myReader;

            using (SqlConnection myCon = new SqlConnection(sqlDatasource))
            {
                try
                {
                    myCon.Open();
                    using (SqlCommand myCommand = new SqlCommand(query, myCon))
                    {
                        myReader = myCommand.ExecuteReader();
                        table.Load(myReader);
                        myReader.Close();
                    }
                    myCon.Close();

                    return new JsonResult(new { success = true, modules = table });
                }
                catch (Exception ex) 
                {
                    return new JsonResult(new { success = false, error = "Error: " + ex.Message });
                }
            }
        }

        [HttpGet]
        [Route("CollectCompanies")]

        public JsonResult CollectCompanies()
        {
            string query = "select * from dbo.COMPANY";

            DataTable table = new DataTable();
            string sqlDatasource = connString;
            SqlDataReader myReader;

            using (SqlConnection myCon = new SqlConnection(sqlDatasource))
            {
                try
                {
                    myCon.Open();
                    using (SqlCommand myCommand = new SqlCommand(query, myCon))
                    {
                        myReader = myCommand.ExecuteReader();
                        table.Load(myReader);
                        myReader.Close();
                    }
                    myCon.Close();

                    return new JsonResult(new { success = true, companies = table });
                }
                catch (Exception ex)
                {
                    return new JsonResult(new { success = false, error = "Error: " + ex.Message });
                }
            }
        }
    }
}
