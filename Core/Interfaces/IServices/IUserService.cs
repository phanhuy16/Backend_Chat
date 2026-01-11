
using Core.DTOs.Users;
using Core.Enums;

namespace Core.Interfaces.IServices
{
    public interface IUserService
    {
        Task<UserDto> GetUserByIdAsync(int userId);
        Task<UserDto> GetUserByUsernameAsync(string username);
        Task<IEnumerable<UserDto>> SearchUsersAsync(string searchTerm);
        Task UpdateUserStatusAsync(int userId, StatusUser status);
        Task<UserDto> UpdateProfileAsync(int userId, UpdateProfileRequest request);
        Task<UserDto> UpdateAvatarAsync(int userId, string avatarUrl);
        Task<bool> UpdateCustomStatusAsync(int userId, string? customStatus);
        Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword);
        Task<bool> DeleteAccountAsync(int userId);
    }
}
