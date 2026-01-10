using Core.DTOs.Friends;

namespace Core.Interfaces.IServices
{
    public interface IFriendService
    {
        // Send friend request
        Task<bool> SendFriendRequestAsync(int senderId, int receiverId);

        // Accept friend request
        Task<bool> AcceptFriendRequestAsync(int requestId, int userId);

        // Reject friend request
        Task<bool> RejectFriendRequestAsync(int requestId, int userId);

        // Get pending friend requests (received)
        Task<IEnumerable<FriendRequestDto>> GetPendingRequestsAsync(int userId);

        // Get sent friend requests
        Task<IEnumerable<FriendRequestDto>> GetSentRequestsAsync(int userId);

        // Get friends list
        Task<IEnumerable<FriendDto>> GetFriendsAsync(int userId);

        // Remove friend
        Task<bool> RemoveFriendAsync(int userId, int friendId);

        // Check if users are friends
        Task<bool> AreFriendsAsync(int userId1, int userId2);
    }
}
