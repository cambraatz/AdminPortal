using AdminPortal.Server.Models;
using AdminPortal.Server.Services.Interfaces;
using System.Data;
using System.Data.SqlClient;

namespace AdminPortal.Server.Services
{
    public class SessionService : ISessionService
    {
        private readonly string? _connString;
        private readonly ILogger<SessionService> _logger;

        public SessionService(IConfiguration config, ILogger<SessionService> logger)
        {
            _connString = config.GetConnectionString("TCS");
            _logger = logger;
        }

        public async Task<SessionModel?> AddOrUpdateSessionAsync(
            long userId,
            string username,
            string accessToken,
            string refreshToken,
            DateTime expiryTime,
            string? powerUnit,
            string? mfstDate)
        {
            try
            {
                string sql;
                long userId_DB = userId;
                using (var conn = new SqlConnection(_connString))
                {
                    await conn.OpenAsync();
                    if (userId == 0)
                    {
                        sql = @"INSERT INTO dbo.SESSIONS (USERNAME, ACCESSTOKEN, REFRESHTOKEN, EXPIRYTIME, LOGINTIME, LASTACTIVITY, POWERUNIT, MFSTDATE)
                        VALUES (@USERNAME, @ACCESSTOKEN, @REFRESHTOKEN, @EXPIRYTIME, @LOGINTIME, @LASTACTIVITY, @POWERUNIT, @MFSTDATE);
                        SELECT SCOPE_IDENTITY();";
                    }
                    else
                    {
                        sql = @"UPDATE dbo.SESSIONS SET 
                                USERNAME = @USERNAME,
                                ACCESSTOKEN = @ACCESSTOKEN,                             
                                REFRESHTOKEN = @REFRESHTOKEN, 
                                EXPIRYTIME = @EXPIRYTIME, 
                                LASTACTIVITY = @LASTACTIVITY, 
                                POWERUNIT = @POWERUNIT, 
                                MFSTDATE = @MFSTDATE 
                            WHERE ID = @ID;";
                    }

                    using (var command = new SqlCommand(sql, conn))
                    {
                        command.Parameters.AddWithValue("@USERNAME", username);
                        command.Parameters.AddWithValue("@ACCESSTOKEN", accessToken);
                        command.Parameters.AddWithValue("@REFRESHTOKEN", refreshToken);
                        command.Parameters.AddWithValue("@EXPIRYTIME", expiryTime);
                        command.Parameters.AddWithValue("@LASTACTIVITY", DateTime.UtcNow); // Always update last activity
                        command.Parameters.Add("@POWERUNIT", SqlDbType.NVarChar, 50).Value = (object?)powerUnit ?? DBNull.Value;
                        command.Parameters.Add("@MFSTDATE", SqlDbType.NVarChar, 8).Value = (object?)mfstDate ?? DBNull.Value;

                        if (userId == 0)
                        {
                            command.Parameters.AddWithValue("@LOGINTIME", DateTime.UtcNow); // Set login time only for new sessions
                            // ExecuteScalarAsync returns the first column of the first row (SCOPE_IDENTITY())
                            userId_DB = Convert.ToInt64(await command.ExecuteScalarAsync());
                            _logger.LogInformation("New session created for user {Username} with ID: {SessionId}", username, userId_DB);
                        }
                        else
                        {
                            command.Parameters.AddWithValue("@ID", userId); // Add ID parameter for update
                            var rowsAffected = await command.ExecuteNonQueryAsync();
                            if (rowsAffected > 0)
                            {
                                _logger.LogInformation("Session ID {SessionId} updated for user {Username}.", userId, username);
                            }
                            else
                            {
                                _logger.LogWarning("Session ID {SessionId} not found for update for user {Username}.", userId, username);
                                // If update failed (e.g., ID not found), you might want to consider it a failure.
                                return null;
                            }
                        }
                    }
                }
                return await GetSessionByIdAsync(userId_DB);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add or update session for user {Username}, ID: {UserId}. Error: {Message}", username, userId, ex.Message);
                return null;
            }
        }
        public async Task<bool> UpdateSessionLastActivityByIdAsync(long sessionId)
        {
            if (string.IsNullOrEmpty(_connString))
            {
                _logger.LogError("Connection string is not configured.");
                return false;
            }
            try
            {
                using (var conn = new SqlConnection(_connString))
                {
                    await conn.OpenAsync();
                    var sql = @"
                        UPDATE dbo.SESSIONS
                        SET LASTACTIVITY = @LASTACTIVITY
                        WHERE ID = @ID";
                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@LASTACTIVITY", DateTime.UtcNow);
                        cmd.Parameters.AddWithValue("@ID", sessionId);
                        var rowsAffected = await cmd.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update session last activity for session ID {SessionId}. Error: {Message}", sessionId, ex.Message);
                return false;
            }
        }
        public async Task<SessionModel?> GetSessionByIdAsync(long userId)
        {
            if (string.IsNullOrEmpty(_connString))
            {
                _logger.LogError("Connection string is not configured.");
                return null;
            }
            try
            {
                using (var conn = new SqlConnection(_connString))
                {
                    await conn.OpenAsync();
                    var query = @"
                        SELECT ID, USERNAME, ACCESSTOKEN, REFRESHTOKEN, EXPIRYTIME, LOGINTIME, LASTACTIVITY, POWERUNIT, MFSTDATE 
                        FROM dbo.SESSIONS WHERE ID = @ID";
                    using (var comm = new SqlCommand(query, conn))
                    {
                        comm.Parameters.AddWithValue("@ID", userId);
                        using (var reader = await comm.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return new SessionModel
                                {
                                    Id = reader.GetInt64(reader.GetOrdinal("ID")),
                                    Username = reader.GetString(reader.GetOrdinal("USERNAME")),
                                    AccessToken = reader.GetString(reader.GetOrdinal("ACCESSTOKEN")),
                                    RefreshToken = reader.GetString(reader.GetOrdinal("REFRESHTOKEN")),
                                    ExpiryTime = reader.GetDateTime(reader.GetOrdinal("EXPIRYTIME")),
                                    LoginTime = reader.GetDateTime(reader.GetOrdinal("LOGINTIME")),
                                    LastActivity = reader.GetDateTime(reader.GetOrdinal("LASTACTIVITY")),
                                    PowerUnit = reader.IsDBNull(reader.GetOrdinal("POWERUNIT")) ? null : reader.GetString(reader.GetOrdinal("POWERUNIT")),
                                    MfstDate = reader.IsDBNull(reader.GetOrdinal("MFSTDATE")) ? null : reader.GetString(reader.GetOrdinal("MFSTDATE"))
                                };
                            }
                            return null;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get session by ID {UserId}. Error: {Message}", userId, ex.Message);
                return null;
            }
        }

        public async Task<SessionModel?> GetSessionAsync(string username, string accessToken, string refreshToken)
        {
            try
            {
                using (var conn = new SqlConnection(_connString))
                {
                    await conn.OpenAsync();
                    var query = @"
                        SELECT ID, USERNAME, ACCESSTOKEN, REFRESHTOKEN, EXPIRYTIME, LOGINTIME, LASTACTIVITY, POWERUNIT, MFSTDATE 
                        FROM dbo.SESSIONS WHERE USERNAME = @USERNAME 
                            AND ACCESSTOKEN = @ACCESSTOKEN 
                            AND REFRESHTOKEN = @REFRESHTOKEN";

                    using (var comm = new SqlCommand(query, conn))
                    {
                        comm.Parameters.AddWithValue("@USERNAME", username);
                        comm.Parameters.AddWithValue("@ACCESSTOKEN", accessToken);
                        comm.Parameters.AddWithValue("@REFRESHTOKEN", refreshToken);

                        using (var reader = await comm.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return new SessionModel
                                {
                                    Id = reader.GetInt64(reader.GetOrdinal("ID")),
                                    Username = reader.GetString(reader.GetOrdinal("USERNAME")),
                                    AccessToken = reader.GetString(reader.GetOrdinal("ACCESSTOKEN")),
                                    RefreshToken = reader.GetString(reader.GetOrdinal("REFRESHTOKEN")),
                                    ExpiryTime = reader.GetDateTime(reader.GetOrdinal("EXPIRYTIME")),
                                    LoginTime = reader.GetDateTime(reader.GetOrdinal("LOGINTIME")),
                                    LastActivity = reader.GetDateTime(reader.GetOrdinal("LASTACTIVITY")),
                                    PowerUnit = reader.IsDBNull(reader.GetOrdinal("POWERUNIT")) ? null : reader.GetString(reader.GetOrdinal("POWERUNIT")),
                                    MfstDate = reader.IsDBNull(reader.GetOrdinal("MFSTDATE")) ? null : reader.GetString(reader.GetOrdinal("MFSTDATE"))
                                };
                            }
                            return null;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get session for user {Username}. Error: {Message}", username, ex.Message);
                return null;
            }
        }

        public async Task<bool> DeleteUserSessionByIdAsync(long sessionId)
        {
            try
            {
                using (var connection = new SqlConnection(_connString))
                {
                    await connection.OpenAsync();
                    var sql = "DELETE FROM dbo.SESSIONS WHERE ID = @ID";
                    using (var command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@ID", sessionId);
                        var rowsAffected = await command.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete session with ID {SessionId}. Error: {Message}", sessionId, ex.Message);
                return false;
            }
        }

        public async Task CleanupExpiredSessionsAsync(TimeSpan idleTimeout)
        {
            try
            {
                using (var connection = new SqlConnection(_connString))
                {
                    await connection.OpenAsync();
                    var now = DateTime.UtcNow;
                    var idleThreshold = now.Subtract(idleTimeout);
                    var sql = @"
                        DELETE FROM dbo.SESSIONS WHERE EXPIRYTIME <= @CURRTIME
                        OR (LASTACTIVITY <= @CURRTIME AND LASTACTIVITY < @IDLETHRESHOLD)        
                    ";
                    using (var command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@CURRTIME", DateTime.UtcNow);
                        command.Parameters.AddWithValue("@IDLETHRESHOLD", idleThreshold);
                        var rowsAffected = await command.ExecuteNonQueryAsync();
                        _logger.LogInformation("Cleaned up {RowsAffected} expired sessions.", rowsAffected);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during expired session cleanup. Error: {Message}", ex.Message);
            }
        }
    }
}
