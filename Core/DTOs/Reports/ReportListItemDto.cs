using Core.Enums;

namespace Core.DTOs.Reports
{
    public class ReportListItemDto
    {
        public int Id { get; set; }
        public string ReportedUserName { get; set; } = string.Empty;
        public string ReporterName { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime ReportedAt { get; set; }
        public ReportStatus Status { get; set; }
    }
}
