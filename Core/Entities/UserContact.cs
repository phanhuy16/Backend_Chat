using Core.Enums;

namespace Core.Entities
{
    public class UserContact
    {
        public int Id { get; set; }

        // User who sends the friend request
        public int SenderId { get; set; }
        public User Sender { get; set; } = null!;

        // User who receives the friend request
        public int ReceiverId { get; set; }
        public User Receiver { get; set; } = null!;
        public StatusFriend Status { get; set; } = StatusFriend.Pending;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // For accepted friendships, track when they became friends
        public DateTime? BecomeFriendAt { get; set; }
    }
}
