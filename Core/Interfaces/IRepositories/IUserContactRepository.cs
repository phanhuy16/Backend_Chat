using Core.Entities;

namespace Core.Interfaces.IRepositories
{
    public interface IUserContactRepository : IRepository<UserContact>
    {
        // Get friend request between two users
        Task<UserContact?> GetFriendRequestAsync(int senderId, int receiverId);

        // Get pending friend requests for a user (received)
        Task<IEnumerable<UserContact>> GetPendingRequestsAsync(int userId);

        // Get sent friend requests for a user
        Task<IEnumerable<UserContact>> GetSentRequestsAsync(int userId);

        // Get accepted friends for a user
        Task<IEnumerable<UserContact>> GetFriendsAsync(int userId);

        // Check if two users are friends
        Task<bool> AreFriendsAsync(int userId1, int userId2);

        // Get all contacts (both sent and received)
        Task<IEnumerable<UserContact>> GetUserContactsAsync(int userId);
    }
}
