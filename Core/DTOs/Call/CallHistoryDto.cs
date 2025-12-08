using Core.DTOs.Users;
using Core.Enums;

namespace Core.DTOs.Call
{
    public class CallHistoryDto
    {
        public int Id { get; set; }
        public string CallId { get; set; } = string.Empty;
        public CallType CallType { get; set; }
        public CallStatus Status { get; set; }
        public int DurationInSeconds { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public UserDto? Initiator { get; set; }
        public UserDto? Receiver { get; set; }
    }
}
