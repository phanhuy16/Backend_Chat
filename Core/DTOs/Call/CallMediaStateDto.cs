namespace Core.DTOs.Call
{
    public class CallMediaStateDto
    {
        public string CallId { get; set; } = string.Empty;
        public int UserId { get; set; }
        public bool IsMuted { get; set; }
        public bool IsVideoOff { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
