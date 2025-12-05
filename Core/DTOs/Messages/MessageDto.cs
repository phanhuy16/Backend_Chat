

using Core.DTOs.Attachments;
using Core.DTOs.Users;
using Core.Enums;

namespace Core.DTOs.Messages
{
    public class MessageDto
    {
        public int Id { get; set; }
        public int ConversationId { get; set; }
        public int SenderId { get; set; }
        public UserDto Sender { get; set; } = new UserDto();
        public string? Content { get; set; }
        public MessageType MessageType { get; set; }
        public DateTime CreatedAt { get; set; }
        public ICollection<ReactionDto> Reactions { get; set; } = null!;
        public ICollection<AttachmentDto> Attachments { get; set; } = null!;
    }
}
