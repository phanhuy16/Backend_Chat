using Core.Enums;

namespace Core.DTOs.Call
{
    public class CallOffer
    {
        public string CallId { get; set; } = string.Empty;
        public int CallerId { get; set; }
        public string CallerName { get; set; } = string.Empty;
        public string? CallerAvatar { get; set; }
        public int ReceiverId { get; set; }
        public int ConversationId { get; set; }
        public CallType CallType { get; set; } = CallType.Audio; // audio | video
        public object Sdp { get; set; } = null!; // RTCSessionDescription
        public long Timestamp { get; set; }
    }
}
