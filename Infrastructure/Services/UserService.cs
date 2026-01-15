

using Core.DTOs.Users;
using Core.Entities;
using Core.Enums;
using Core.Interfaces.IRepositories;
using Core.Interfaces.IServices;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<UserService> _logger;

        public UserService(IUserRepository userRepository, ILogger<UserService> logger)
        {
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                    return false;

                if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
                {
                    _logger.LogWarning($"User {userId} provided incorrect current password");
                    return false;
                }

                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
                user.UpdatedAt = DateTime.UtcNow;

                await _userRepository.UpdateAsync(user);
                _logger.LogInformation($"User {userId} password changed");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Error changing password: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> DeleteAccountAsync(int userId)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                    return false;

                if (!string.IsNullOrEmpty(user.Avatar) && user.Avatar.StartsWith("/wwwroot/"))
                {
                    DeleteAvatarFile(user.Avatar);
                }

                await _userRepository.DeleteAsync(userId);
                _logger.LogInformation($"User {userId} account deleted");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting account: {ex.Message}");
                throw;
            }
        }

        public async Task<UserDto> GetUserByIdAsync(int userId)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("User {UserId} not found", userId);
                    return null!;
                }
                return MapToUserDto(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by ID {UserId}", userId);
                throw;
            }
        }

        public async Task<UserDto> GetUserByUsernameAsync(string username)
        {
            try
            {
                var user = await _userRepository.GetByUsernameAsync(username);
                if (user == null)
                {
                    _logger.LogWarning("User with username {Username} not found", username);
                    return null!;
                }
                return MapToUserDto(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by username {Username}", username);
                throw;
            }
        }

        public async Task<IEnumerable<UserDto>> SearchUsersAsync(string searchTerm)
        {
            try
            {
                var users = await _userRepository.SearchUsersAsync(searchTerm);
                return users.Select(MapToUserDto).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching users with term '{SearchTerm}'", searchTerm);
                throw;
            }
        }

        public async Task<UserDto> UpdateAvatarAsync(int userId, string avatarUrl)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                    return null!;

                if (string.IsNullOrEmpty(user.Avatar))
                {
                    DeleteAvatarFile(user.Avatar);
                }

                user.Avatar = avatarUrl;
                user.UpdatedAt = DateTime.UtcNow;

                await _userRepository.UpdateAsync(user);
                _logger.LogInformation($"User {userId} avatar updated");

                return MapToUserDto(user);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating avatar: {ex.Message}");
                throw;
            }
        }

        public async Task<UserDto> UpdateProfileAsync(int userId, UpdateProfileRequest request)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                    return null!;

                if (!string.IsNullOrWhiteSpace(request.DisplayName))
                    user.DisplayName = request.DisplayName;

                if (request.Bio != null) user.Bio = request.Bio;
                if (request.Avatar != null) user.Avatar = request.Avatar;

                if (request.LastSeenPrivacy != null) user.LastSeenPrivacy = request.LastSeenPrivacy;
                if (request.OnlineStatusPrivacy != null) user.OnlineStatusPrivacy = request.OnlineStatusPrivacy;
                if (request.ReadReceiptsEnabled.HasValue) user.ReadReceiptsEnabled = request.ReadReceiptsEnabled.Value;

                user.UpdatedAt = DateTime.UtcNow;

                await _userRepository.UpdateAsync(user);
                _logger.LogInformation($"User {userId} profile updated");

                return MapToUserDto(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile for user {UserId}", userId);
                throw;
            }
        }

        public async Task UpdateUserStatusAsync(int userId, StatusUser status)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("User {UserId} not found for status update", userId);
                    return;
                }

                user.Status = status;
                user.UpdatedAt = DateTime.UtcNow;
                await _userRepository.UpdateAsync(user);

                _logger.LogInformation("User {UserId} status updated to {Status}", userId, status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating status for user {UserId}", userId);
                throw;
            }
        }

        private void DeleteAvatarFile(string avatarUrl)
        {
            try
            {
                var relativePath = avatarUrl.TrimStart('/');
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", relativePath);

                var wwwrootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                var fullPath = Path.GetFullPath(filePath);
                var fullWwwrootPath = Path.GetFullPath(wwwrootPath);

                if (!fullPath.StartsWith(fullWwwrootPath))
                {
                    _logger.LogWarning($"Attempted to delete file outside uploads directory: {filePath}");
                    return;
                }

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    _logger.LogInformation($"Avatar file deleted: {filePath}");
                }
                else
                {
                    _logger.LogWarning($"Avatar file not found: {filePath}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting avatar file: {ex.Message}");
            }
        }

        public async Task<bool> UpdateCustomStatusAsync(int userId, string? customStatus)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) return false;

            user.CustomStatus = customStatus;
            user.UpdatedAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);
            return true;
        }

        private UserDto MapToUserDto(User user)
        {
            return new UserDto
            {
                Id = user.Id,
                UserName = user.UserName!,
                Email = user.Email!,
                DisplayName = user.DisplayName,
                Avatar = user.Avatar,
                Bio = user.Bio,
                Status = user.Status,
                CustomStatus = user.CustomStatus,
                LastActiveAt = user.UpdatedAt,
                LastSeenPrivacy = user.LastSeenPrivacy,
                OnlineStatusPrivacy = user.OnlineStatusPrivacy,
                ReadReceiptsEnabled = user.ReadReceiptsEnabled
            };
        }
    }
}
