

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
        public List<AttachmentDto> Attachments { get; set; } = new List<AttachmentDto>();
        public bool IsDeleted { get; set; }
        public bool IsDeletedForMe { get; set; }
        public bool IsPinned { get; set; }
        public bool IsModified { get; set; }
        public int? ParentMessageId { get; set; }
        public MessageDto? ParentMessage { get; set; }
        public int? ForwardedFromId { get; set; }
        public bool IsReadByMe { get; set; }
        public int ReadCount { get; set; }
        public DateTime? ScheduledAt { get; set; }
        public int? PollId { get; set; }
        public PollDto? Poll { get; set; }

        // Self-destructing message fields
        public int? SelfDestructAfterSeconds { get; set; }
        public DateTime? ViewedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }

        public List<UserDto> MentionedUsers { get; set; } = new List<UserDto>();
    }
}
