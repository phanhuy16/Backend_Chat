using Core.Enums;

namespace Core.DTOs.Messages
{
    public class SendMessageRequest
    {
        public int ConversationId { get; set; }
        public int SenderId { get; set; }
        public string? Content { get; set; }
        public MessageType MessageType { get; set; }
    }
}
