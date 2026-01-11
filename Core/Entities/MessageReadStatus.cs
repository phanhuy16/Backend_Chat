using System;

namespace Core.Entities
{
    public class MessageReadStatus
    {
        public int Id { get; set; }
        public int MessageId { get; set; }
        public int UserId { get; set; }
        public DateTime ReadAt { get; set; }

        public Message Message { get; set; } = null!;
        public User User { get; set; } = null!;
    }
}
