namespace Core.DTOs.Conversations
{
    public class LinkDto
    {
        public int MessageId { get; set; }
        public string Url { get; set; } = string.Empty;
        public string? Title { get; set; } // Optional: For future expansion (OG tags)
        public string SenderName { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }
    }
}
