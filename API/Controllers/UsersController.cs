using Core.DTOs.Users;
using Core.Interfaces.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<UsersController> _logger;

        public UsersController(IUserService userService, ILogger<UsersController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        /// <summary>
        /// Get user by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(int id)
        {
            try
            {
                var user = await _userService.GetUserByIdAsync(id);
                if (user == null)
                {
                    _logger.LogWarning("User {UserId} not found", id);
                    return NotFound("User not found");
                }
                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching user {UserId}", id);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get user by username
        /// </summary>
        [HttpGet("username/{username}")]
        public async Task<IActionResult> GetUserByUsername(string username)
        {
            try
            {
                var user = await _userService.GetUserByUsernameAsync(username);
                if (user == null)
                {
                    _logger.LogWarning("User {Username} not found", username);
                    return NotFound("User not found");
                }
                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching user by username {Username}", username);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Search users
        /// </summary>
        [HttpGet("search")]
        public async Task<IActionResult> SearchUsers([FromQuery] string searchTerm)
        {
            if (string.IsNullOrEmpty(searchTerm))
            {
                _logger.LogWarning("Search term is required");
                return BadRequest("Search term is required");
            }

            try
            {
                var users = await _userService.SearchUsersAsync(searchTerm);
                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching users with term {SearchTerm}", searchTerm);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Update user profile (name, bio, etc.)
        /// </summary>
        [HttpPut("profile")]
        [Authorize]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
        {
            var userId = GetCurrentUserId();
            if (userId <= 0)
                return Unauthorized("User not found");

            if (string.IsNullOrWhiteSpace(request.DisplayName))
                return BadRequest("Display name is required");

            try
            {
                var user = await _userService.UpdateProfileAsync(userId, request);
                if (user == null)
                    return NotFound("User not found");

                _logger.LogInformation($"User {userId} profile updated");
                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating profile: {ex.Message}");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// Change user password
        /// </summary>
        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            var userId = GetCurrentUserId();
            if (userId <= 0)
                return Unauthorized("User not found");

            if (string.IsNullOrWhiteSpace(request.CurrentPassword) || string.IsNullOrWhiteSpace(request.NewPassword))
                return BadRequest("Current password and new password are required");

            if (request.NewPassword.Length < 6)
                return BadRequest("New password must be at least 6 characters");

            try
            {
                var result = await _userService.ChangePasswordAsync(userId, request.CurrentPassword, request.NewPassword);
                if (!result)
                    return BadRequest("Failed to change password. Current password may be incorrect.");

                _logger.LogInformation($"User {userId} password changed");
                return Ok(new { message = "Password changed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error changing password: {ex.Message}");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// Upload user avatar
        /// </summary>
        [HttpPost("upload-avatar")]
        [Authorize]
        public async Task<IActionResult> UploadAvatar([FromForm] UploadAvatarRequest request)
        {
            var file = request.File;

            var userId = GetCurrentUserId();
            if (userId <= 0)
                return Unauthorized("User not found");

            if (file == null || file.Length == 0)
                return BadRequest("File is required");

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(fileExtension))
                return BadRequest("Invalid file type. Only jpg, jpeg, png, gif are allowed");

            if (file.Length > 5 * 1024 * 1024)
                return BadRequest("File size exceeds 5MB limit");

            try
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "avatars");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName = $"{userId}_{DateTime.UtcNow.Ticks}{fileExtension}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var avatarUrl = $"/avatars/{uniqueFileName}";
                var user = await _userService.UpdateAvatarAsync(userId, avatarUrl);

                if (user == null)
                    return NotFound("User not found");

                _logger.LogInformation($"User {userId} avatar uploaded");
                return Ok(new { avatarUrl = avatarUrl, user = user });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error uploading avatar: {ex.Message}");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// Delete user account
        /// </summary>
        [HttpDelete("account")]
        [Authorize]
        public async Task<IActionResult> DeleteAccount()
        {
            var userId = GetCurrentUserId();
            if (userId <= 0)
                return Unauthorized("User not found");

            try
            {
                var result = await _userService.DeleteAccountAsync(userId);
                if (!result)
                    return BadRequest("Failed to delete account");

                _logger.LogInformation($"User {userId} account deleted");
                return Ok(new { message = "Account deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting account: {ex.Message}");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// Update user status
        /// </summary>
        [HttpPut("{userId}/status")]
        [Authorize]
        public async Task<IActionResult> UpdateUserStatus(int userId, [FromBody] UpdateStatusRequest request)
        {
            if (userId <= 0)
                return BadRequest("Invalid user ID");

            try
            {
                await _userService.UpdateUserStatusAsync(userId, request.Status);
                _logger.LogInformation($"User {userId} status updated to {request.Status}");
                return Ok(new { message = "Status updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating status: {ex.Message}");
                return StatusCode(500, new { message = ex.Message });
            }
        }
        /// <summary>
        /// Update user custom status message
        /// </summary>
        [HttpPut("custom-status")]
        [Authorize]
        public async Task<IActionResult> UpdateCustomStatus([FromBody] UpdateCustomStatusRequest request)
        {
            var userId = GetCurrentUserId();
            if (userId <= 0)
                return Unauthorized("User not found");

            try
            {
                var result = await _userService.UpdateCustomStatusAsync(userId, request.CustomStatus);
                if (!result)
                    return NotFound("User not found");

                _logger.LogInformation($"User {userId} custom status updated");
                return Ok(new { message = "Custom status updated successfully", customStatus = request.CustomStatus });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating custom status: {ex.Message}");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                return userId;
            }
            return 0;
        }
        public class UploadAvatarRequest
        {
            public IFormFile File { get; set; } = null!;
        }

    }
}
