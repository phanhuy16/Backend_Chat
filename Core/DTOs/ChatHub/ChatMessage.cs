using Core.Enums;

namespace Core.DTOs.ChatHub
{
    public class ChatMessage
    {
        public int MessageId { get; set; }
        public int ConversationId { get; set; }
        public int SenderId { get; set; }
        public string SenderName { get; set; } = string.Empty;
        public string SenderAvatar { get; set; } = string.Empty;
        public string? Content { get; set; }
        public MessageType MessageType { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? ParentMessageId { get; set; }
        public ChatMessage? ParentMessage { get; set; }
        public PollDto? Poll { get; set; }
        public List<int> MentionedUserIds { get; set; } = new List<int>();
    }
}
