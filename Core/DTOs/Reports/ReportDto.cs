using Core.DTOs.Users;
using Core.Enums;

namespace Core.DTOs.Reports
{
    public class ReportDto
    {
        public int Id { get; set; }
        public int ReportedUserId { get; set; }
        public int ReporterId { get; set; }
        public int? ConversationId { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime ReportedAt { get; set; }
        public ReportStatus Status { get; set; }
        public string? AdminNotes { get; set; }

        // User info
        public UserDto? ReportedUser { get; set; }
        public UserDto? Reporter { get; set; }
    }
}
