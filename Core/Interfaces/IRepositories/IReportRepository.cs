using Core.Entities;

namespace Core.Interfaces.IRepositories
{
    public interface IReportRepository : IRepository<Report>
    {
        Task<Report?> GetReportWithDetailsAsync(int reportId);
        Task<List<Report>> GetUserReportsAsync(int reportedUserId);
        Task<List<Report>> GetPendingReportsAsync();
        Task<List<Report>> GetReportsByReporterAsync(int reporterId);
        Task<bool> UserAlreadyReportedAsync(int reporterId, int reportedUserId);
    }
}
