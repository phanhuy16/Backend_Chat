using System;

namespace Core.DTOs.Messages
{
    public class MessageReaderDto
    {
        public int UserId { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string Avatar { get; set; } = string.Empty;
        public DateTime ReadAt { get; set; }
    }
}
