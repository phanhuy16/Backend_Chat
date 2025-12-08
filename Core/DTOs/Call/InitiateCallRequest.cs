namespace Core.DTOs.Call
{
    public class InitiateCallRequest
    {
        public int ReceiverId { get; set; }
        public int ConversationId { get; set; }
        public string CallType { get; set; } = "Audio";
    }
}
