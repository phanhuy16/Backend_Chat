namespace Core.DTOs.Reports
{
    public class CreateReportDto
    {
        public int ReportedUserId { get; set; }
        public int ReporterId { get; set; }
        public int? ConversationId { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
