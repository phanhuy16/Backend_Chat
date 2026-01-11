using Core.DTOs.Messages;
using Core.DTOs.Users;
using Core.Enums;

namespace Core.DTOs.Conversations
{
    public class ConversationDto
    {
        public int Id { get; set; }
        public ConversationType ConversationType { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public int CreatedBy { get; set; }
        public List<UserDto> Members { get; set; } = new List<UserDto>();
        public List<MessageDto> Messages { get; set; } = new List<MessageDto>();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public bool IsPinned { get; set; }
    }
}
