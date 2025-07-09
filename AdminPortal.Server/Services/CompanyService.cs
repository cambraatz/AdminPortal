using AdminPortal.Server.Models;
using AdminPortal.Server.Services;
using AdminPortal.Server.Services.Interfaces;
using System.Data.SqlClient;

namespace AdminPortal.Server.Services
{
    public class CompanyService : ICompanyService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<CompanyService> _logger;
        private readonly string _connString;

        public CompanyService(IConfiguration config,
            ILogger<CompanyService> logger)
        {
            _config = config;
            _logger = logger;
            _connString = _config.GetConnectionString("TCS") ??
                throw new InvalidOperationException("TCS connection string is not configured.");
        }

        // fetch full company name from company key...
        public async Task<(string? companyName, string message)> GetCompanyNameAsync(string companyKey)
        {
            const string query = @"
                select COMPANYNAME FROM dbo.COMPANY
                where COMPANYKEY COLLATE SQL_Latin1_General_CP1_CS_AS = @COMPANYKEY";

            try
            {
                await using var conn = new SqlConnection(_connString);
                await conn.OpenAsync();

                await using var comm = new SqlCommand(query, conn);
                comm.Parameters.AddWithValue("@COMPANYKEY", companyKey);

                // execute query and ensure valid result...
                string? companyName = (string?)await comm.ExecuteScalarAsync();
                if (companyName == null)
                {
                    _logger.LogWarning($"Attempted to retrieve company name for '{companyKey}', but no company found with that key.");
                    return (null, $"Company with key '{companyKey}' not found.");
                }

                return (companyName, $"Company name '{companyName}' on record under key '{companyKey}.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Unexpected error while fetching company '{companyKey}'.");
                return (null, $"Unexpected error while fetching company '{companyKey}'.");
            }
        }

        // get the company db key using the full company name...
        public async Task<(string? companyKey, string message)> GetCompanyKeyAsync(string companyName)
        {
            const string query = @"
                            select COMPANYKEY FROM dbo.COMPANY
                            where COMPANYNAME COLLATE SQL_Latin1_General_CP1_CS_AS = @COMPANYNAME";
            try
            {
                await using var conn = new SqlConnection(_connString);
                await conn.OpenAsync();

                await using var comm = new SqlCommand(query, conn);
                comm.Parameters.AddWithValue("@COMPANYNAME", companyName);

                // execute query and ensure valid result...
                string? companyKey = (string?)await comm.ExecuteScalarAsync();
                if (companyKey == null)
                {
                    _logger.LogWarning($"Attempted to retrieve company key for '{companyName}', but no company found with that name.");
                    return (null, $"Company with name '{companyName}' not found.");
                }

                return (companyKey, $"Company key '{companyKey}' on record under name '{companyName}.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Unexpected error while fetching company '{companyName}'.");
                return (null, $"Unexpected error while fetching company '{companyName}'.");
            }
        }

        // update the full company name of the active company db key...
        public async Task<(bool success, string message)> UpdateCompanyAsync(string companyKey, string newName)
        {
            // fetch company name using company db key...
            (string? companyName, string? message) = await GetCompanyNameAsync(companyKey);
            if (string.IsNullOrEmpty(companyName))
            {
                _logger.LogInformation($"Failed to retrieve company name using '{companyKey}'.");
                return (false, message);
            }

            // check if new company already exists for another company...
            const string companyQuery = @"
                                select COUNT(*) from dbo.COMPANY 
                                where COMPANYNAME COLLATE SQL_Latin1_General_CP1_CS_AS = @NEWCOMPANYNAME
                                AND COMPANYKEY COLLATE SQL_Latin1_General_CP1_CS_AS <> @COMPANYKEY;";

            // update company in recors...
            const string updateQuery = @"
                                update dbo.COMPANY 
                                set COMPANYNAME=@NEWCOMPANYNAME 
                                where COMPANYKEY COLLATE SQL_Latin1_General_CP1_CS_AS = @COMPANYKEY";

            try
            {
                await using var conn = new SqlConnection(_connString);
                await conn.OpenAsync();

                // if company name was updated to new name...
                if (!string.Equals(companyName, newName, StringComparison.OrdinalIgnoreCase))
                {
                    await using (var comm = new SqlCommand(companyQuery, conn))
                    {
                        comm.Parameters.AddWithValue("@NEWCOMPANYNAME", newName);
                        comm.Parameters.AddWithValue("@COMPANYKEY", companyKey);

                        // execute company query and handle duplicate value error...
                        int? existingCount = (int?)await comm.ExecuteScalarAsync();
                        if (existingCount != null && existingCount > 0)
                        {
                            _logger.LogWarning($"Attempted to update company '{companyKey}' to '{newName}', but another company already has it in use.");
                            return (false, "Company name already exists for another company.");
                        }
                    }
                }

                // company update query init...
                await using (var comm = new SqlCommand(updateQuery, conn))
                {
                    comm.Parameters.AddWithValue("@NEWCOMPANYNAME", newName);
                    comm.Parameters.AddWithValue("@COMPANYKEY", companyKey);

                    // execute query and ensure valid update...
                    int rowsAffected = await comm.ExecuteNonQueryAsync();
                    if (rowsAffected > 0)
                    {
                        _logger.LogInformation($"Company '{companyKey}' successfully updated to '{newName}'.");
                        return (true, $"Company '{companyKey}' updated to '{newName}' successfully.");
                    }
                    else
                    {
                        _logger.LogWarning($"Attempted to update company '{companyKey}', but update command affected 0 rows (possible concurrency issue or no change).");
                        return (false, "Company not found or no changes were made.");
                    }
                }
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, $"SQL error updating company '{companyKey}' to '{newName}': {sqlEx.Message}");
                return (false, "A database error occurred during the update.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error updating company '{companyKey}' to '{newName}': {ex.Message}");
                return (false, "An unexpected error occurred during the update.");
            }
        }
    }
}
