using Core.Entities;
using Core.Interfaces.IRepositories;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Repositories
{
    public class BlockedUserRepository : Repository<BlockedUser>, IBlockedUserRepository
    {
        private readonly ILogger<BlockedUserRepository> _logger;

        public BlockedUserRepository(
            ChatAppDbContext context,
            ILogger<BlockedUserRepository> logger) : base(context)
        {
            _logger = logger;
        }

        public async Task<BlockedUser?> GetAsync(int blockerId, int blockedUserId)
        {
            try
            {
                _logger.LogInformation(
                    "Fetching blocked user. BlockerId: {BlockerId}, BlockedUserId: {BlockedUserId}",
                    blockerId, blockedUserId);

                var result = await _context.BlockedUsers
                    .Include(b => b.BlockedUserProfile)
                    .FirstOrDefaultAsync(b => b.BlockerId == blockerId && b.BlockedUserId == blockedUserId);

                if (result == null)
                {
                    _logger.LogWarning(
                        "Blocked user not found. BlockerId: {BlockerId}, BlockedUserId: {BlockedUserId}",
                        blockerId, blockedUserId);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error occurred while fetching blocked user. BlockerId: {BlockerId}, BlockedUserId: {BlockedUserId}",
                    blockerId, blockedUserId);

                throw;
            }
        }

        public async Task<List<BlockedUser>> GetBlockedUsersAsync(int blockerId)
        {
            try
            {
                _logger.LogInformation(
                    "Fetching all blocked users for BlockerId: {BlockerId}",
                    blockerId);

                var result = await _context.BlockedUsers
                    .Where(b => b.BlockerId == blockerId)
                    .Include(b => b.BlockedUserProfile)
                    .ToListAsync();

                _logger.LogInformation(
                    "Found {Count} blocked users for BlockerId: {BlockerId}",
                    result.Count, blockerId);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error occurred while fetching blocked users. BlockerId: {BlockerId}",
                    blockerId);

                throw;
            }
        }

        public async Task<bool> IsUserBlockedAsync(int userId1, int userId2)
        {
            try
            {
                _logger.LogInformation(
                    "Checking if users are blocked. UserId1: {UserId1}, UserId2: {UserId2}",
                    userId1, userId2);

                // Check both directions:
                // 1. userId1 blocked userId2 (userId1 -> userId2)
                // 2. userId2 blocked userId1 (userId2 -> userId1)
                var isBlocked = await _context.BlockedUsers
                    .AnyAsync(b =>
                        (b.BlockerId == userId1 && b.BlockedUserId == userId2) ||
                        (b.BlockerId == userId2 && b.BlockedUserId == userId1));

                return isBlocked;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error checking if users are blocked. UserId1: {UserId1}, UserId2: {UserId2}",
                    userId1, userId2);
                throw;
            }
        }

        public async Task<BlockedUser?> GetBlockerInfoAsync(int userId1, int userId2)
        {
            try
            {
                var blockRecord = await _context.BlockedUsers
                    .Include(b => b.Blocker)
                    .FirstOrDefaultAsync(b =>
                        (b.BlockerId == userId1 && b.BlockedUserId == userId2) ||
                        (b.BlockerId == userId2 && b.BlockedUserId == userId1));

                return blockRecord;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting blocker info");
                throw;
            }
        }
    }
}
