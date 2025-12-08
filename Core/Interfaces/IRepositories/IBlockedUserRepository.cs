using Core.Entities;

namespace Core.Interfaces.IRepositories
{
    public interface IBlockedUserRepository : IRepository<BlockedUser>
    {
        Task<BlockedUser?> GetAsync(int blockerId, int blockedUserId);
        Task<List<BlockedUser>> GetBlockedUsersAsync(int blockerId);
        Task<bool> IsUserBlockedAsync(int userId1, int userId2);
        Task<BlockedUser?> GetBlockerInfoAsync(int userId1, int userId2);
    }
}
