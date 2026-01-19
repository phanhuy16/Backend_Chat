using Core.Entities;
using Core.Interfaces.IRepositories;
using Core.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Repositories
{
    public class UserRepository : Repository<User>, IUserRepository
    {
        private readonly ILogger<UserRepository> _logger;

        public UserRepository(ChatAppDbContext context, ILogger<UserRepository> logger)
            : base(context)
        {
            _logger = logger;
        }

        public async Task<User?> GetByUsernameAsync(string username)
        {
            try
            {
                return await _context.Users
                    .FirstOrDefaultAsync(u => u.UserName == username);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi truy vấn User theo username: {Username}", username);
                return null;
            }
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            try
            {
                return await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi truy vấn User theo email: {Email}", email);
                return null;
            }
        }

        public async Task<User?> GetByPasswordResetTokenAsync(string token)
        {
            try
            {
                return await _context.Users
                    .FirstOrDefaultAsync(u => u.PasswordResetToken == token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi truy vấn User theo reset token");
                return null;
            }
        }

        public async Task<User?> GetByFacebookIdAsync(string facebookId)
        {
            try
            {
                return await _context.Users
                    .FirstOrDefaultAsync(u => u.FacebookId == facebookId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi truy vấn User theo Facebook ID: {FacebookId}", facebookId);
                return null;
            }
        }

        public async Task<IEnumerable<User>> SearchUsersAsync(string searchTerm)
        {
            try
            {
                return await _context.Users
                    .Where(u =>
                        (u.UserName != null && u.UserName.Contains(searchTerm)) ||
                        (u.DisplayName != null && u.DisplayName.Contains(searchTerm))
                    )
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tìm kiếm user với keyword: {SearchTerm}", searchTerm);
                return Enumerable.Empty<User>();
            }
        }

        public async Task<IEnumerable<User>> SearchUsersAsync(string searchTerm, int pageSize)
        {
            try
            {
                return await _context.Users
                    .Where(u =>
                        (u.UserName != null && u.UserName.Contains(searchTerm)) ||
                        (u.DisplayName != null && u.DisplayName.Contains(searchTerm))
                    )
                    .Take(pageSize)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tìm kiếm user với keyword: {SearchTerm}", searchTerm);
                return Enumerable.Empty<User>();
            }
        }

        public async Task<IEnumerable<User>> GetContactsAsync(int userId)
        {
            try
            {
                var contacts = await _context.UserContacts
                    .Where(uc => (uc.SenderId == userId || uc.ReceiverId == userId) && uc.Status == StatusFriend.Accepted)
                    .Select(uc => uc.SenderId == userId ? uc.ReceiverId : uc.SenderId)
                    .ToListAsync();

                return await _context.Users
                    .Where(u => contacts.Contains(u.Id))
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách contacts của user: {UserId}", userId);
                return Enumerable.Empty<User>();
            }
        }
    }
}
