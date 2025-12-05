using Core.Entities;

namespace Core.Interfaces.IRepositories
{
    public interface IBlockedUserRepository : IRepository<BlockedUser>
    {
        Task<BlockedUser?> GetAsync(int blockerId, int blockedUserId);
        Task<List<BlockedUser>> GetBlockedUsersAsync(int blockerId);
    }
}
