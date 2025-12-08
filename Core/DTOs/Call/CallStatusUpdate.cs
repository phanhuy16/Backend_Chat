using Core.Enums;

namespace Core.DTOs.Call
{
    public class CallStatusUpdate
    {
        public string CallId { get; set; } = string.Empty;
        public CallStatus Status { get; set; }  // ringing, connecting, connected, ended, rejected
        public int? UserId { get; set; }
    }
}
