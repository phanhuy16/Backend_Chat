namespace Core.DTOs.Call
{
    public class CallEndDto
    {
        public string CallId { get; set; } = string.Empty;
        public int Duration { get; set; } // in seconds
        public DateTime EndedAt { get; set; }
    }
}
