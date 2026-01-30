using Core.Enums;

namespace Core.DTOs.Messages
{
    public class SendMessageRequest
    {
        public int ConversationId { get; set; }
        public int SenderId { get; set; }
        public string? Content { get; set; }
        public MessageType MessageType { get; set; }
        public int? ParentMessageId { get; set; }
        public List<int> MentionedUserIds { get; set; } = new List<int>();
        public int? SelfDestructAfterSeconds { get; set; }  // Optional: time in seconds before message self-destructs after being viewed
    }
}
