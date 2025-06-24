using AdminPortal.Server.Models;

namespace AdminPortal.Server.Services.Interfaces
{
    public interface IUserService
    {
        Task<UserCredentials?> GetByUsernameAsync(string username);
        Task<(bool success, string message, User? generatedUser)> AddUserAsync(User newUser);
        Task<(bool success, string message, UserCredentials? updatedUser)> UpdateUserAsync(string oldUsername, UserCredentials newUser);
        Task<(bool success, string message)> DeleteUserAsync(string username);
    }
}
