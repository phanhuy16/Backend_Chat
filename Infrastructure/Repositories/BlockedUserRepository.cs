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
    }
}
