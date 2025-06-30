using AdminPortal.Server.Models;
using AdminPortal.Server.Services;
using AdminPortal.Server.Services.Interfaces;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Data;
using System.Data.SqlClient;
using System.Text.Json;

namespace AdminPortal.Server.Controllers
{
    [ApiController]
    [Route("v1/users")]
    public class UserController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly IHostEnvironment _env;
        private readonly IUserService _userService;
        private readonly ICookieService _cookieService;
        private readonly ILogger<UserController> _logger;
        private readonly string _connString;

        public UserController(IConfiguration config,
            IHostEnvironment env,
            IUserService userService,
            ICookieService cookieService,
            ILogger<UserController> logger
            )
        {
            _config = config;
            _env = env;
            _userService = userService;
            _cookieService = cookieService;
            _logger = logger;
            _connString = _config.GetConnectionString("TCS")!;
        }

        // ["v1/users"] generates user...
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateUser([FromBody] User newUser)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for CreateUser: {Errors}", ModelState);
                return BadRequest(ModelState); // Returns a 400 with validation errors
            }

            (bool success, string message, User? user) = await _userService.AddUserAsync(newUser);
            if (success)
            {
                return CreatedAtAction(nameof(GetUserByUsername), new { username = user?.Username }, user);
            }
            else if (message.Contains("Username already exists"))
            {
                return Conflict(new { message = message });
            }
            else
            {
                _logger.LogError("Error creating user: {Message}", message);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = message });
            }
        }

        // fetches user by username...
        [HttpGet]
        [Route("{username}")]
        [Authorize]
        public async Task<IActionResult> GetUserByUsername(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return BadRequest("Username cannot be empty.");
            }

            UserCredentials? validUser = await _userService.GetByUsernameAsync(username);
            if (validUser == null)
            {
                return NotFound($"User with username '{username}' not found.");
            }

            User user = new User()
            {
                Username = validUser.Username,
                Permissions = null,
                Powerunit = validUser.Powerunit,
                ActiveCompany = validUser.Companies?.First(),
                Companies = validUser.Companies,
                Modules = validUser.Modules
            };

            return Ok(user);
        }

        // update existing user accessible at previous username...
        [HttpPut]
        [Route("{prevUsername}")]
        [Authorize]
        public async Task<IActionResult> UpdateUser([FromBody] UserCredentials newUser, string prevUsername)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for CreateUser: {Errors}", ModelState);
                return BadRequest(ModelState); // 400
            }

            (bool success, string message, UserCredentials? updatedUser) = await _userService.UpdateUserAsync(prevUsername, newUser);
            if (success)
            {
                // update user cookies if currUser edited their own records...
                var currUser = Request.Cookies["username"];
                if (!string.IsNullOrEmpty(updatedUser?.Username) &&
                    !string.IsNullOrEmpty(currUser) &&
                    currUser.Equals(prevUsername, StringComparison.OrdinalIgnoreCase))
                {
                    Response.Cookies.Append("username", updatedUser.Username, _cookieService.AccessOptions());
                }

                if (updatedUser != null)
                {
                    User user = new User()
                    {
                        Username = updatedUser.Username,
                        Permissions = null,
                        Powerunit = updatedUser.Powerunit,
                        ActiveCompany = updatedUser.Companies?.First(),
                        Companies = updatedUser.Companies,
                        Modules = updatedUser.Modules
                    };

                    return Ok(updatedUser);
                }

                _logger.LogWarning("Update failed gracefully, additional error handling needed here.");
                return BadRequest(ModelState); // 400

            }
            else
            {
                if (message.Contains("Original user not found."))
                {
                    _logger.LogInformation("Attempt to update non-existent user '{OldUsername}'.", prevUsername);
                    return NotFound($"User with username '{prevUsername}' not found."); // 404
                }
                else if (message.Contains("Username is already in use."))
                {
                    _logger.LogWarning($"Conflict: New username '{newUser.Username}' for user '{prevUsername}' already exists.");
                    return Conflict(new { message = message }); // 409
                }
                else if (message.Contains("Powerunit is already in use."))
                {
                    _logger.LogWarning($"Conflict: New powerunit '{newUser.Powerunit}' for user '{prevUsername}' already exists.");
                    return Conflict(new { message = message }); // 409
                }
                else
                {
                    _logger.LogError("Error updating user '{OldUsername}': {ErrorMessage}", prevUsername, message);
                    return StatusCode(StatusCodes.Status500InternalServerError, new { message = message }); // 500
                }
            }
        }

        // remove existing user accessible at username...
        [HttpDelete]
        [Route("{username}")]
        [Authorize]
        public async Task<IActionResult> DeleteUser(string username)
        {
            var currUser = Request.Cookies["username"];
            if (!string.IsNullOrEmpty(currUser) && currUser.Equals(username, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Conflict: Active username '{Username}' cannot be deleted while in use.", username);
                return Conflict(new { message = $"Active username '{username}' cannot be deleted while in use." }); // 409
            }

            (bool success, string message) = await _userService.DeleteUserAsync(username);
            if (success)
            {
                _logger.LogInformation("'{Username}' was deleted successfully from records.", username);
                return NoContent(); // 204
            }
            else
            {
                if (message.Contains("User not found."))
                {
                    _logger.LogInformation("Attempt to delete non-existent user '{Username}'.", username);
                    return NotFound($"User with username '{username}' not found."); // 404
                }
                else 
                {
                    _logger.LogError("Error deleting user '{Username}': {ErrorMessage}", username, message);
                    return StatusCode(StatusCodes.Status500InternalServerError, new { message = message }); // 500
                }
            }
        }
    }
}
