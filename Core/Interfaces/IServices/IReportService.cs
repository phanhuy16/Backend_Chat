using Core.DTOs.Reports;
using Core.Enums;

namespace Core.Interfaces.IServices
{
    public interface IReportService
    {
        Task<ReportDto> CreateReportAsync(CreateReportDto reportDto);
        Task<ReportDto> GetReportAsync(int reportId);
        Task<List<ReportListItemDto>> GetUserReportsAsync(int userId);
        Task<List<ReportListItemDto>> GetPendingReportsAsync();
        Task<List<ReportListItemDto>> GetReportsByReporterAsync(int reporterId);
        Task UpdateReportStatusAsync(int reportId, ReportStatus status, string? adminNotes);
        Task DeleteReportAsync(int reportId);
    }
}
