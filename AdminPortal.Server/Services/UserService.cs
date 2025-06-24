using AdminPortal.Server.Models;
using AdminPortal.Server.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using System.Data;
using System.Data.SqlClient;

namespace AdminPortal.Server.Services
{
    public class UserService : IUserService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<UserService> _logger;
        private readonly string _connString;

        public UserService(IConfiguration config, ILogger<UserService> logger)
        {
            _config = config;
            _logger = logger;
            _connString = _config.GetConnectionString("TCS") ?? 
                throw new InvalidOperationException("TCS connection string is not configured.");
        }

        // HELPER: fetch user by username from records...
        private async Task<UserCredentials?> FetchUserAsync(string sqlQuery, Action<SqlParameterCollection> addParams)
        {
            await using var conn = new SqlConnection(_connString);
            await conn.OpenAsync();

            await using var comm = new SqlCommand(sqlQuery, conn);
            addParams(comm.Parameters);

            await using var reader = await comm.ExecuteReaderAsync(CommandBehavior.SingleRow);
            if (!reader.Read()) { return null; }

            // package user credentials from query...
            var user = new UserCredentials
            {
                Username = reader["USERNAME"].ToString(),
                Password = reader["PASSWORD"].ToString(),
                Powerunit = reader["POWERUNIT"].ToString(),
                Companies = new(),
                Modules = new()
            };

            // iteratively compile company + module lists...
            for (int i = 1; i <= 5; i++)
            {
                var key = reader[$"COMPANYKEY0{i}"]?.ToString();
                if (!string.IsNullOrEmpty(key))
                {
                    user.Companies.Add(key);
                }
            }
            for (int i = 1; i <= 10; i++)
            {
                var mod = reader[$"MODULE{i:D2}"]?.ToString();
                if (!string.IsNullOrEmpty(mod))
                {
                    user.Modules.Add(mod);
                }
            }

            return user;
        }

        // fetch username from records...
        public async Task<UserCredentials?> GetByUsernameAsync(string username)
        {
            // fetch user from provided username...
            const string query = @" select USERNAME, PASSWORD, PERMISSIONS, POWERUNIT,
                            COMPANYKEY01, COMPANYKEY02, COMPANYKEY03, COMPANYKEY04, COMPANYKEY05,
                            MODULE01, MODULE02, MODULE03, MODULE04, MODULE05, MODULE06, MODULE07, MODULE08, MODULE09, MODULE10
                            from dbo.USERS where USERNAME COLLATE SQL_Latin1_General_CP1_CS_AS = @USERNAME";
            try
            {
                // leverage helper with arrow func...
                UserCredentials? user = await FetchUserAsync(query, p => p.AddWithValue("@USERNAME", username));
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database error retrieving user '{Username}': {ErrorMessage}", username, ex.Message);
                return null;
            }
        }

        // add new user to records...
        public async Task<(bool success, string message, User? generatedUser)> AddUserAsync(User newUser)
        {
            // block invalid username param...
            if (string.IsNullOrWhiteSpace(newUser.Username))
            {
                return (false, "Username is required.", null);
            }

            // [1] sum cases of duplicate username|powerunit already in records...
            const string conflictQuery = @"
                SELECT
                    SUM(CASE WHEN USERNAME COLLATE SQL_Latin1_General_CP1_CS_AS = @USERNAME THEN 1 ELSE 0 END) AS UsernameCount,
                    SUM(CASE WHEN POWERUNIT COLLATE SQL_Latin1_General_CP1_CS_AS = @POWERUNIT THEN 1 ELSE 0 END) AS PowerunitCount
                FROM dbo.USERS";

            // [2] insert new user into records...
            const string insertQuery = @"
                INSERT INTO dbo.USERS(
                    USERNAME, PASSWORD, POWERUNIT,
                    COMPANYKEY01, COMPANYKEY02, COMPANYKEY03, COMPANYKEY04, COMPANYKEY05,
                    MODULE01, MODULE02, MODULE03, MODULE04, MODULE05,
                    MODULE06, MODULE07, MODULE08, MODULE09, MODULE10
                ) VALUES (
                    @USERNAME, @PASSWORD, @POWERUNIT,
                    @COMPANYKEY01, @COMPANYKEY02, @COMPANYKEY03, @COMPANYKEY04, @COMPANYKEY05,
                    @MODULE01, @MODULE02, @MODULE03, @MODULE04, @MODULE05,
                    @MODULE06, @MODULE07, @MODULE08, @MODULE09, @MODULE10)";
            try
            {
                // initialize sql connection...
                await using var conn = new SqlConnection(_connString);
                await conn.OpenAsync();

                // check for duplicates...
                await using (SqlCommand comm = new SqlCommand(conflictQuery, conn)) // [1]...
                {
                    comm.Parameters.AddWithValue("@USERNAME", newUser.Username);
                    comm.Parameters.AddWithValue("@POWERUNIT", newUser.Powerunit);
                    
                    // execute query...
                    await using (var reader = await comm.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            // collect summed duplicates and catch conflicts...
                            int usernameConflicts = reader.GetInt32(reader.GetOrdinal("UsernameCount"));
                            int powerunitConflicts = reader.GetInt32(reader.GetOrdinal("PowerunitCount"));

                            if (usernameConflicts > 0)
                            {
                                return (false, "Username is already in use by another user.", null);
                            }
                            if (powerunitConflicts > 0)
                            {
                                return (false, "Powerunit is already assigned to another user", null);
                            }
                        }
                    }
                }

                // insert new user into records...
                await using (SqlCommand comm = new SqlCommand(insertQuery, conn)) // [2]...
                {
                    comm.Parameters.AddWithValue("@USERNAME", newUser.Username);
                    comm.Parameters.AddWithValue("@PASSWORD", DBNull.Value);
                    comm.Parameters.AddWithValue("@POWERUNIT", newUser.Powerunit != null ? newUser.Powerunit : DBNull.Value);

                    // iterate company + module lists and isolate params...
                    for (int i = 0; i < 5; i++)
                    {
                        string paramName = $"@COMPANYKEY0{i + 1}";
                        if (i < newUser.Companies.Count && newUser.Companies[i] != null)
                        {
                            comm.Parameters.AddWithValue(paramName, newUser.Companies[i]);
                        }
                        else
                        {
                            comm.Parameters.AddWithValue(paramName, DBNull.Value);
                        }
                    }
                    for (int i = 0; i < 10; i++)
                    {
                        string paramName = $"@MODULE{(i + 1).ToString("00")}"; // Ensures "01", "02", ..., "10"
                        if (i < newUser.Modules.Count && newUser.Modules[i] != null)
                        {
                            comm.Parameters.AddWithValue(paramName, newUser.Modules[i]);
                        }
                        else
                        {
                            comm.Parameters.AddWithValue(paramName, DBNull.Value);
                        }
                    }

                    // execute query and ensure valid addition...
                    int rowsAffected = await comm.ExecuteNonQueryAsync();
                    if (rowsAffected > 0)
                    {
                        _logger.LogInformation("Successfully created new user: {Username}", newUser.Username);
                        return (true, "User created successfully.", newUser);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to insert user {Username}. No rows affected.", newUser.Username);
                        return (false, "Failed to create user. No rows affected.", null);
                    }
                }
                
            }
            catch (SqlException sqlex)
            {
                _logger.LogError(sqlex, "Database error adding user {Username}: {Message}", newUser.Username, sqlex.Message);

                // handle specific sql errors if needed (ie: unique constraint violation)...
                if (sqlex.Number == 2601 || sqlex.Number == 2627)
                {
                    return (false, "Username already exists.", null);
                }
                return (false, $"Database error: {sqlex.Message}", null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while adding user {Username}: {Message}", newUser.Username, ex.Message);
                return (false, $"An unexpected error occurred: {ex.Message}", null);
            }
        }

        // update existing user in records...
        public async Task<(bool success, string message, UserCredentials? updatedUser)> UpdateUserAsync(string oldUsername, UserCredentials newUser)
        {
            // block invalid username param...
            if (string.IsNullOrWhiteSpace(newUser.Username))
            {
                return (false, "Username is required.", null);
            }

            // [1] fetch powerunit + username from records...
            const string usernameQuery = @"SELECT POWERUNIT, USERNAME FROM dbo.USERS WHERE USERNAME COLLATE SQL_Latin1_General_CP1_CS_AS = @OLDUSERNAME";

            // [2] sum cases of duplicate username|powerunit already in records, unless belonging to self...
            const string conflictQuery = @"
                SELECT
                    SUM(CASE WHEN USERNAME COLLATE SQL_Latin1_General_CP1_CS_AS = @NEWUSERNAME THEN 1 ELSE 0 END) AS UsernameCount,
                    SUM(CASE WHEN @NEWPOWERUNIT IS NOT NULL AND POWERUNIT COLLATE SQL_Latin1_General_CP1_CS_AS = @NEWPOWERUNIT THEN 1 ELSE 0 END) AS PowerunitCount
                FROM dbo.USERS
                WHERE USERNAME COLLATE SQL_Latin1_General_CP1_CS_AS <> @OLDUSERNAME";

            // [3] update existing user in records...
            const string updateQuery = @"
                UPDATE dbo.USERS
                SET
                    USERNAME = @NEWUSERNAME,
                    PASSWORD = @PASSWORD,
                    POWERUNIT = @POWERUNIT,
                    COMPANYKEY01 = @COMPANYKEY01, COMPANYKEY02 = @COMPANYKEY02, COMPANYKEY03 = @COMPANYKEY03, COMPANYKEY04 = @COMPANYKEY04, COMPANYKEY05 = @COMPANYKEY05,
                    MODULE01 = @MODULE01, MODULE02 = @MODULE02, MODULE03 = @MODULE03, MODULE04 = @MODULE04, MODULE05 = @MODULE05, MODULE06 = @MODULE06, MODULE07 = @MODULE07, MODULE08 = @MODULE08, MODULE09 = @MODULE09, MODULE10 = @MODULE10
                WHERE USERNAME COLLATE SQL_Latin1_General_CP1_CS_AS = @OLDUSERNAME";

            try
            {
                // initialize sql connection...
                await using var conn = new SqlConnection(_connString);
                await conn.OpenAsync();

                // fetch prev powerunit + username to check for duplicates...
                string? db_OldPowerunit = null;
                string? db_OldUsername = null;
                await using (SqlCommand comm = new SqlCommand(usernameQuery, conn)) // [1]...
                {
                    comm.Parameters.AddWithValue("@OLDUSERNAME", oldUsername);
                    using (var reader = await comm.ExecuteReaderAsync())
                    {
                        // initialize powerunit + username if valid...
                        if (await reader.ReadAsync())
                        {
                            db_OldPowerunit = reader["POWERUNIT"] == DBNull.Value ? null : reader["POWERUNIT"].ToString();
                            db_OldUsername = reader["USERNAME"].ToString();
                        }
                        else
                        {
                            return (false, "Original user not found.", null);
                        }
                    }
                }

                // cache powerunit on change...
                object newPowerunit = string.IsNullOrEmpty(newUser.Powerunit) ? DBNull.Value : (object)newUser.Powerunit;

                // check for powerunit + username conflicts...
                await using (SqlCommand comm = new SqlCommand(conflictQuery, conn)) // [2]...
                {
                    comm.Parameters.AddWithValue("@NEWUSERNAME", newUser.Username);
                    comm.Parameters.AddWithValue("@OLDUSERNAME", oldUsername);
                    comm.Parameters.AddWithValue("@NEWPOWERUNIT", newPowerunit);

                    await using (var reader = await comm.ExecuteReaderAsync())
                    {
                        // catch duplicate value conflicts...
                        if (await reader.ReadAsync())
                        {
                            // fetch duplicate counts...
                            int usernameConflicts = reader.GetInt32(reader.GetOrdinal("UsernameCount"));
                            int powerunitConflicts = reader.GetInt32(reader.GetOrdinal("PowerunitCount"));

                            // if powerunit|username changed, but duplicate value was found... 
                            if (!string.Equals(db_OldUsername, newUser.Username, StringComparison.OrdinalIgnoreCase) && 
                                usernameConflicts > 0)
                            {
                                return (false, "Username is already in use by another user.", null);
                            }
                            if (newPowerunit != DBNull.Value && 
                                !string.Equals(db_OldPowerunit, newUser.Powerunit, StringComparison.OrdinalIgnoreCase) &&
                                powerunitConflicts > 0)
                            {
                                return (false, "Powerunit is already assigned to another user", null);
                            }
                        }
                    }
                }

                // update user in database...
                await using (SqlCommand comm = new SqlCommand(updateQuery, conn)) // [3]...
                {
                    comm.Parameters.AddWithValue("@OLDUSERNAME", oldUsername);
                    comm.Parameters.AddWithValue("@NEWUSERNAME", newUser.Username);
                    comm.Parameters.AddWithValue("@PASSWORD", string.IsNullOrEmpty(newUser.Password) ? DBNull.Value : newUser.Password);
                    comm.Parameters.AddWithValue("@POWERUNIT", newUser.Powerunit);

                    // iteratively add company + module list params...
                    for (int i = 0; i < 5; i++)
                    {
                        string paramName = $"@COMPANYKEY0{i + 1}";
                        object compValue = (newUser.Companies != null && i < newUser.Companies.Count && newUser.Companies[i] != null) 
                            ? (object)newUser.Companies[i] 
                            :  DBNull.Value;

                        comm.Parameters.AddWithValue(paramName, compValue);
                    }
                    for (int i = 0; i < 10; i++)
                    {
                        string paramName = $"@MODULE{(i + 1).ToString("00")}";
                        object modValue = (newUser.Modules != null && i < newUser.Modules.Count && newUser.Modules[i] != null)
                        ? (object)newUser.Modules[i]
                        : DBNull.Value;

                        comm.Parameters.AddWithValue(paramName, modValue);
                    }

                    // add permission values here if active...
                    // updateCommand.Parameters.AddWithValue("@PERMISSIONS", string.IsNullOrEmpty(userUpdateData.Permissions) ? DBNull.Value : userUpdateData.Permissions);

                    // execute query and ensuer valid update...
                    int rowsAffected = await comm.ExecuteNonQueryAsync();
                    if (rowsAffected == 0)
                    {
                        return (false, "User not found or no changes were made.", null);
                    }

                    // if valid, fetch user credentials...
                    UserCredentials? updatedUser = await GetByUsernameAsync(newUser.Username);
                    if (updatedUser != null)
                    {
                        /* POTENTIAL DANGER: password data is being passed, admin access to this function ONLY */
                        return (true, "User updated successfully.", updatedUser);
                    }
                    return (false, "User updated failed gracefully, additional handling needed here.", null);
                }
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, "SQL error updating user '{OldUsername}' to '{NewUsername}': {ErrorMessage}", oldUsername, newUser.Username, sqlEx.Message);
                return (false, "A database error occurred during the update.", null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error updating user '{OldUsername}' to '{NewUsername}': {ErrorMessage}", oldUsername, newUser.Username, ex.Message);
                return (false, "An unexpected error occurred.", null);
            }
        }

        // delete existing user from records...
        public async Task<(bool success, string message)> DeleteUserAsync(string username)
        {
            // block invalid username params...
            if (string.IsNullOrWhiteSpace(username))
            {
                return (false, "Username is required.");
            }

            // delete user matching provided username...
            string query = "delete from dbo.USERS where USERNAME COLLATE SQL_Latin1_General_CP1_CS_AS = @USERNAME";

            try
            {
                await using var conn = new SqlConnection(_connString);
                await conn.OpenAsync();

                await using var comm = new SqlCommand(query, conn);
                comm.Parameters.AddWithValue("@USERNAME", username);

                int rowsAffected = await comm.ExecuteNonQueryAsync();
                if (rowsAffected > 0)
                {
                    // log success...
                    _logger.LogInformation("User '{Username}' deleted successfully from database.", username);
                    return (true, $"User '{username}' deleted successfully.");
                }
                else
                {
                    // log fail...
                    _logger.LogInformation("Attempted to delete user '{Username}', but user was not found.", username);
                    return (false, "User not found.");
                }
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, "SQL error deleting user '{Username}': {ErrorMessage}", username, sqlEx.Message);
                return (false, "A database error occurred during deletion.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error deleting user '{Username}': {ErrorMessage}", username, ex.Message);
                return (false, "An unexpected error occurred during deletion.");
            }
        }
    }
}
