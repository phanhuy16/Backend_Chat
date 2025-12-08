namespace Core.DTOs.Call
{
    public class CallAnswer
    {
        public string CallId { get; set; } = string.Empty;
        public int ReceiverId { get; set; }
        public object Sdp { get; set; } = null!; // RTCSessionDescription
    }
}
