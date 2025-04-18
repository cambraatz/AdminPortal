/*/////////////////////////////////////////////////////////////////////////////

Author: Cameron Braatz
Date: 3/12/2025
Update: --/--/----

*//////////////////////////////////////////////////////////////////////////////

using AdminPortal.Server.Models;
using AdminPortal.Server.Services;
using static AdminPortal.Server.Services.TokenService;

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
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Threading.Tasks;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.DataProtection.KeyManagement;

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

        public AdminController(IConfiguration configuration, TokenService tokenService, ILogger<AdminController> logger)
        {
            _configuration = configuration;
            //_tokenService = tokenService;
            connString = _configuration.GetConnectionString("TCSWEB");
            _logger = logger;
        }

        /*[HttpPost]
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
        }*/

        [HttpPost]
        [Route("Logout")]
        public IActionResult Logout()
        {
            foreach (var cookie in Request.Cookies)
            {
                Response.Cookies.Append(cookie.Key, cookie.Value, CookieService.RemoveOptions());
            };

            return Ok(new { message = "Logged out successfully" });
        }

        [HttpPost]
        [Route("Return")]
        public IActionResult Return()
        {
            CookieService.ExtendCookies(HttpContext, 15);
            Response.Cookies.Append("return", "true", CookieService.AccessOptions());

            return Ok(new { message = "Returning, cookies extended by 15 minutes." });
        }

        [HttpPost]
        [Route("ValidateUser")]
        public async Task<JsonResult> ValidateUser()
        {
            var tokenService = new TokenService(_configuration);
            (bool success, string message) tokenAuth = tokenService.AuthorizeRequest(HttpContext);
            if (!tokenAuth.success)
            {
                return new JsonResult(new { success = false, message = tokenAuth.message }) { StatusCode = StatusCodes.Status401Unauthorized };
            }

            var username = Request.Cookies["username"];
            if (string.IsNullOrEmpty(username))
            {
                return new JsonResult(new { success = false, message = "Username is missing" }) { StatusCode = StatusCodes.Status401Unauthorized };
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

                return new JsonResult(new { success = true, user = user });
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
            var tokenService = new TokenService(_configuration);
            (bool success, string message) tokenAuth = tokenService.AuthorizeRequest(HttpContext);
            if (!tokenAuth.success)
            {
                return new JsonResult(new { success = false, message = tokenAuth.message }) { StatusCode = StatusCodes.Status401Unauthorized };
            }

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
            var tokenService = new TokenService(_configuration);
            (bool success, string message) tokenAuth = tokenService.AuthorizeRequest(HttpContext);
            if (!tokenAuth.success)
            {
                return new JsonResult(new { success = false, message = tokenAuth.message }) { StatusCode = StatusCodes.Status401Unauthorized };
            }

            //string query = "SELECT USERNAME, PASSWORD, POWERUNIT FROM dbo.USERS WHERE USERNAME = @USERNAME";
            string query = "SELECT * FROM dbo.USERS WHERE USERNAME = @USERNAME";

            DataTable table = new DataTable();
            string sqlDatasource = connString;
            driverCredentials driver = new driverCredentials();

            await using (SqlConnection myCon = new SqlConnection(sqlDatasource))
            {
                try
                {
                    await myCon.OpenAsync();
                    using (SqlCommand myCommand = new SqlCommand(query, myCon))
                    {
                        myCommand.Parameters.AddWithValue("@USERNAME", request.USERNAME);
                        using (SqlDataReader myReader = await myCommand.ExecuteReaderAsync())
                        {
                            if (!myReader.HasRows)
                            {
                                return new JsonResult(new { success = false, message = "User not found." });
                            }
                            myReader.Read();

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
                            
                            myReader.Close();
                        }
                    }
                    myCon.Close();

                    return new JsonResult(new
                    {
                        success = true,
                        username = driver.USERNAME,
                        password = driver.PASSWORD,
                        powerunit = driver.POWERUNIT,
                        user = driver
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
        public async Task<JsonResult> ReplaceDriver([FromBody] driverReplacement driver)
        {
            var tokenService = new TokenService(_configuration);
            (bool success, string message) tokenAuth = tokenService.AuthorizeRequest(HttpContext);
            if (!tokenAuth.success)
            {
                return new JsonResult(new { success = false, message = tokenAuth.message }) { StatusCode = StatusCodes.Status401Unauthorized };
            }

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
                        return new JsonResult(new { success = false, value = "Retrieving username from cookies failed." }) { StatusCode = StatusCodes.Status401Unauthorized };
                    }

                    if (user == driver.PrevUser)
                    {
                        Response.Cookies.Append("username", driver.Username, CookieService.AccessOptions());

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
        public JsonResult DeleteDriver(string USERNAME)
        {
            var tokenService = new TokenService(_configuration);
            (bool success, string message) tokenAuth = tokenService.AuthorizeRequest(HttpContext);
            if (!tokenAuth.success)
            {
                return new JsonResult(new { success = false, message = tokenAuth.message }) { StatusCode = StatusCodes.Status401Unauthorized };
            }

            var username = Request.Cookies["username"];
            if (string.IsNullOrEmpty(username))
            {
                return new JsonResult(new { success = false, message = "Username is missing" }) { StatusCode = StatusCodes.Status401Unauthorized };
            }
            else if (username.Equals(USERNAME))
            {
                return new JsonResult(new { success = false, duplicate = true, message = "Deleting the active user is not allowed, contact administrator." });
            }

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
                    return new JsonResult(new { success = false, duplicate = false, error = "Error: " + ex.Message });

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

        [HttpPost]
        [Route("FetchMappings")]
        public async Task<JsonResult> FetchMappings()
        {
            try
            {
                var rawCompanies = await FetchCompanies();
                var companies = JsonSerializer.Serialize(rawCompanies);//await FetchCompanies());
                Response.Cookies.Append("company_mapping", companies, CookieService.AccessOptions());

                var rawModules = await FetchModules();
                var modules = JsonSerializer.Serialize(rawModules);//await FetchModules());
                Response.Cookies.Append("module_mapping", modules, CookieService.AccessOptions());

                return new JsonResult(new
                {
                    success = true,
                    companies = companies,
                    modules = modules,
                    message = "Company and Module mappings have been stored in cookies."
                });
            }
            catch (Exception ex)
            {
                return new JsonResult(new
                {
                    success = false,
                    message = $"Company and Module mappings failed; Exception: {ex.Message}"
                });
            }
        }

        private async Task<Dictionary<string, string>?> FetchCompanies()
        {
            var companies = new Dictionary<string, string>();
            string sqlDatasource = connString;

            await using (SqlConnection myCon = new SqlConnection(sqlDatasource))
            {
                try
                {
                    await myCon.OpenAsync();
                    using (SqlCommand myCommand = new SqlCommand("select * from dbo.COMPANY", myCon))
                    {
                        using (SqlDataReader myReader = myCommand.ExecuteReader())
                        {
                            DataTable table = new DataTable();
                            table.Load(myReader);

                            foreach (DataRow row in table.Rows)
                            {
                                var key = row["COMPANYKEY"].ToString();
                                var name = row["COMPANYNAME"].ToString();

                                if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(name))
                                {
                                    companies[key] = name;
                                }
                            }
                        }
                    }

                    await myCon.CloseAsync();
                    return companies;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"An error occurred while fetching companies; Exception: {ex.Message}");
                    return null;
                }
            }
        }
        private async Task<Dictionary<string, string>?> FetchModules()
        {
            var modules = new Dictionary<string, string>();
            string sqlDatasource = connString;

            await using (SqlConnection myCon = new SqlConnection(sqlDatasource))
            {
                try
                {
                    await myCon.OpenAsync();
                    using (SqlCommand myCommand = new SqlCommand("select * from dbo.MODULE", myCon))
                    {
                        using (SqlDataReader myReader = myCommand.ExecuteReader())
                        {
                            DataTable table = new DataTable();
                            table.Load(myReader);

                            foreach (DataRow row in table.Rows)
                            {
                                var key = row["MODULEURL"].ToString();
                                var name = row["MODULENAME"].ToString();

                                if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(name))
                                {
                                    modules[key] = name;
                                }
                            }
                        }
                    }
                    await myCon.CloseAsync();
                    return modules;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"An error occurred while fetching modules; Exception: {ex.Message}");
                    return null;
                }
            }
        }

        // ADMIN FUNCTION...
        [HttpPut]
        [Route("SetCompany")]
        public async Task<JsonResult> SetCompany([FromBody] string COMPANYNAME)
        {
            var tokenService = new TokenService(_configuration);
            (bool success, string message) tokenAuth = tokenService.AuthorizeRequest(HttpContext);
            if (!tokenAuth.success)
            {
                return new JsonResult(new { success = false, message = tokenAuth.message }) { StatusCode = StatusCodes.Status401Unauthorized };
            }

            var company = Request.Cookies["company"];
            if (string.IsNullOrEmpty(company))
            {
                return new JsonResult(new { success = false, message = "Company key is missing." }) { StatusCode = StatusCodes.Status401Unauthorized };
            }

            string query = "update dbo.COMPANY set COMPANYNAME=@COMPANYNAME where COMPANYKEY=@COMPANYKEY";
            string sqlDatasource = connString;

            await using (SqlConnection myCon = new SqlConnection(sqlDatasource))
            {
                try
                {
                    await myCon.OpenAsync();
                    using (SqlCommand myCommand = new SqlCommand(query, myCon))
                    {
                        myCommand.Parameters.AddWithValue("@COMPANYNAME", COMPANYNAME);
                        myCommand.Parameters.AddWithValue("@COMPANYKEY", company);

                        int rowsAffected = await myCommand.ExecuteNonQueryAsync();

                        if (rowsAffected > 0)
                        {
                            return new JsonResult(new { success = true, message = "Company Updated", company = COMPANYNAME });
                        } 
                        else 
                        {
                            return new JsonResult(new { success = false, message = "Error: Update failed, no company matched active company key. Contact administrator." });
                        }
                    }
                }
                catch (Exception ex)
                {
                    return new JsonResult(new { success = false, message = "Error: " + ex.Message });
                }
            }
        }
    }
}
