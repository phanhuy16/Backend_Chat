

using Core.Enums;

namespace Core.DTOs.Friends
{
    public class FriendRequestDto
    {
        public int Id { get; set; }
        public int SenderId { get; set; }
        public string SenderName { get; set; } = string.Empty;
        public string SenderAvatar { get; set; } = string.Empty;
        public int ReceiverId { get; set; }
        public string ReceiverName { get; set; } = string.Empty;
        public string ReceiverAvatar { get; set; } = string.Empty;
        public StatusFriend Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
