using Core.Entities;
using Core.Enums;
using Core.Interfaces.IRepositories;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Repositories
{
    public class ReportRepository : Repository<Report>, IReportRepository
    {
        private readonly ILogger<ReportRepository> _logger;

        public ReportRepository(ChatAppDbContext context, ILogger<ReportRepository> logger)
            : base(context)
        {
            _logger = logger;
        }

        // ✅ Get report with all details
        public async Task<Report?> GetReportWithDetailsAsync(int reportId)
        {
            try
            {
                _logger.LogInformation($"Fetching report {reportId} with details");

                var report = await _context.Reports
                    .Include(r => r.ReportedUser)
                    .Include(r => r.Reporter)
                    .Include(r => r.Conversation)
                    .FirstOrDefaultAsync(r => r.Id == reportId);

                return report;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching report {reportId}");
                throw;
            }
        }

        // ✅ Get all reports for a specific user
        public async Task<List<Report>> GetUserReportsAsync(int reportedUserId)
        {
            try
            {
                _logger.LogInformation($"Fetching reports for user {reportedUserId}");

                var reports = await _context.Reports
                    .Include(r => r.ReportedUser)
                    .Include(r => r.Reporter)
                    .Where(r => r.ReportedUserId == reportedUserId)
                    .OrderByDescending(r => r.ReportedAt)
                    .ToListAsync();

                return reports;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching reports for user {reportedUserId}");
                throw;
            }
        }

        // ✅ Get all pending reports (for admin)
        public async Task<List<Report>> GetPendingReportsAsync()
        {
            try
            {
                _logger.LogInformation("Fetching pending reports");

                var reports = await _context.Reports
                    .Include(r => r.ReportedUser)
                    .Include(r => r.Reporter)
                    .Where(r => r.Status == ReportStatus.Pending)
                    .OrderByDescending(r => r.ReportedAt)
                    .ToListAsync();

                return reports;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching pending reports");
                throw;
            }
        }

        // ✅ Get reports by reporter
        public async Task<List<Report>> GetReportsByReporterAsync(int reporterId)
        {
            try
            {
                _logger.LogInformation($"Fetching reports by reporter {reporterId}");

                var reports = await _context.Reports
                    .Include(r => r.ReportedUser)
                    .Include(r => r.Reporter)
                    .Where(r => r.ReporterId == reporterId)
                    .OrderByDescending(r => r.ReportedAt)
                    .ToListAsync();

                return reports;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching reports by reporter {reporterId}");
                throw;
            }
        }

        // ✅ Check if user already reported someone
        public async Task<bool> UserAlreadyReportedAsync(int reporterId, int reportedUserId)
        {
            try
            {
                var exists = await _context.Reports
                    .AnyAsync(r => r.ReporterId == reporterId
                        && r.ReportedUserId == reportedUserId
                        && (r.Status == ReportStatus.Pending || r.Status == ReportStatus.Reviewed));

                return exists;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking if user {reporterId} already reported {reportedUserId}");
                throw;
            }
        }
    }
}
