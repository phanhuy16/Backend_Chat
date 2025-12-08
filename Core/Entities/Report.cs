using Core.Enums;

namespace Core.Entities
{
    public class Report
    {
        public int Id { get; set; }
        public int ReportedUserId { get; set; }
        public int ReporterId { get; set; }
        public int? ConversationId { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime ReportedAt { get; set; } = DateTime.UtcNow;
        public ReportStatus Status { get; set; } = ReportStatus.Pending;
        public string? AdminNotes { get; set; }

        // Foreign keys
        public User ReportedUser { get; set; } = null!;
        public User Reporter { get; set; } = null!;
        public Conversations? Conversation { get; set; }
    }
}
