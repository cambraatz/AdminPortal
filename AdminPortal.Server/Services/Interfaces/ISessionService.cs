using AdminPortal.Server.Models;

namespace AdminPortal.Server.Services.Interfaces
{
    public interface ISessionService
    {
        Task<SessionModel?> AddOrUpdateSessionAsync(long userId, string username, string accessToken, string refreshToken, DateTime expiryTime, string? powerUnit, string? mfstDate);
        Task<bool> UpdateSessionLastActivityByIdAsync(long sessionId);
        Task<SessionModel?> GetSessionByIdAsync(long userId);
        Task<SessionModel?> GetSessionAsync(string username, string accessToken, string refreshToken);
        Task<bool> DeleteUserSessionByIdAsync(long sessionId);
        Task CleanupExpiredSessionsAsync(TimeSpan idleTimeout);
    }
}
