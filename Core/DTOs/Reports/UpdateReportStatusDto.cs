using Core.Enums;

namespace Core.DTOs.Reports
{
    public class UpdateReportStatusDto
    {
        public int ReportId { get; set; }
        public ReportStatus Status { get; set; }
        public string? AdminNotes { get; set; }
    }
}
