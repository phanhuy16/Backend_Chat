using Core.DTOs.Reports;
using Core.DTOs.Users;
using Core.Entities;
using Core.Enums;
using Core.Interfaces.IRepositories;
using Core.Interfaces.IServices;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services
{
    public class ReportService : IReportService
    {
        private readonly IReportRepository _reportRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<ReportService> _logger;

        public ReportService(
            IReportRepository reportRepository,
            IUserRepository userRepository,
            ILogger<ReportService> logger)
        {
            _reportRepository = reportRepository;
            _userRepository = userRepository;
            _logger = logger;
        }

        // Create a new report
        public async Task<ReportDto> CreateReportAsync(CreateReportDto reportDto)
        {
            try
            {
                // Validate reported user exists
                var reportedUser = await _userRepository.GetByIdAsync(reportDto.ReportedUserId);
                if (reportedUser == null)
                {
                    throw new Exception($"User {reportDto.ReportedUserId} not found");
                }

                // Validate reporter exists
                var reporter = await _userRepository.GetByIdAsync(reportDto.ReporterId);
                if (reporter == null)
                {
                    throw new Exception($"User {reportDto.ReporterId} not found");
                }

                // Check if user can't report themselves
                if (reportDto.ReporterId == reportDto.ReportedUserId)
                {
                    throw new Exception("Cannot report yourself");
                }

                // Check for duplicate reports
                var alreadyReported = await _reportRepository.UserAlreadyReportedAsync(
                    reportDto.ReporterId,
                    reportDto.ReportedUserId);

                if (alreadyReported)
                {
                    throw new Exception("You have already reported this user. Pending review.");
                }

                // Create report
                var report = new Report
                {
                    ReportedUserId = reportDto.ReportedUserId,
                    ReporterId = reportDto.ReporterId,
                    ConversationId = reportDto.ConversationId,
                    Reason = reportDto.Reason,
                    Description = reportDto.Description,
                    ReportedAt = DateTime.UtcNow,
                    Status = ReportStatus.Pending
                };

                await _reportRepository.AddAsync(report);

                _logger.LogInformation(
                    $"Report created: User {reportDto.ReporterId} reported user {reportDto.ReportedUserId} for reason: {reportDto.Reason}");

                return MapToReportDto(report, reportedUser, reporter);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating report");
                throw;
            }
        }

        // Get report by ID
        public async Task<ReportDto> GetReportAsync(int reportId)
        {
            try
            {
                var report = await _reportRepository.GetReportWithDetailsAsync(reportId);
                if (report == null)
                {
                    throw new Exception($"Report {reportId} not found");
                }

                return MapToReportDto(report, report.ReportedUser, report.Reporter);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting report {reportId}");
                throw;
            }
        }

        // Get reports for a user
        public async Task<List<ReportListItemDto>> GetUserReportsAsync(int userId)
        {
            try
            {
                var reports = await _reportRepository.GetUserReportsAsync(userId);

                return reports.Select(r => new ReportListItemDto
                {
                    Id = r.Id,
                    ReportedUserName = r.ReportedUser.DisplayName ?? r.ReportedUser.UserName ?? "Unknown",
                    ReporterName = r.Reporter.DisplayName ?? r.Reporter.UserName ?? "Unknown",
                    Reason = r.Reason,
                    ReportedAt = r.ReportedAt,
                    Status = r.Status
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting reports for user {userId}");
                throw;
            }
        }

        // Get pending reports (for admin)
        public async Task<List<ReportListItemDto>> GetPendingReportsAsync()
        {
            try
            {
                var reports = await _reportRepository.GetPendingReportsAsync();

                return reports.Select(r => new ReportListItemDto
                {
                    Id = r.Id,
                    ReportedUserName = r.ReportedUser.DisplayName ?? r.ReportedUser.UserName ?? "Unknown",
                    ReporterName = r.Reporter.DisplayName ?? r.Reporter.UserName ?? "Unknown",
                    Reason = r.Reason,
                    Description = r.Description,
                    ReportedAt = r.ReportedAt,
                    Status = r.Status
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending reports");
                throw;
            }
        }

        // Get reports by reporter
        public async Task<List<ReportListItemDto>> GetReportsByReporterAsync(int reporterId)
        {
            try
            {
                var reports = await _reportRepository.GetReportsByReporterAsync(reporterId);

                return reports.Select(r => new ReportListItemDto
                {
                    Id = r.Id,
                    ReportedUserName = r.ReportedUser.DisplayName ?? r.ReportedUser.UserName ?? "Unknown",
                    ReporterName = r.Reporter.DisplayName ?? r.Reporter.UserName ?? "Unknown",
                    Reason = r.Reason,
                    Description = r.Description,
                    ReportedAt = r.ReportedAt,
                    Status = r.Status
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting reports by reporter {reporterId}");
                throw;
            }
        }

        // Update report status (admin only)
        public async Task UpdateReportStatusAsync(int reportId, ReportStatus status, string? adminNotes)
        {
            try
            {
                var report = await _reportRepository.GetByIdAsync(reportId);
                if (report == null)
                {
                    throw new Exception($"Report {reportId} not found");
                }

                report.Status = status;
                report.AdminNotes = adminNotes;

                await _reportRepository.UpdateAsync(report);

                _logger.LogInformation($"Report {reportId} status updated to {status}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating report {reportId} status");
                throw;
            }
        }

        // Delete report (admin only)
        public async Task DeleteReportAsync(int reportId)
        {
            try
            {
                await _reportRepository.DeleteAsync(reportId);
                _logger.LogInformation($"Report {reportId} deleted");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting report {reportId}");
                throw;
            }
        }

        // Helper method to map to DTO
        private ReportDto MapToReportDto(Report report, User reportedUser, User reporter)
        {
            return new ReportDto
            {
                Id = report.Id,
                ReportedUserId = report.ReportedUserId,
                ReporterId = report.ReporterId,
                ConversationId = report.ConversationId,
                Reason = report.Reason,
                Description = report.Description,
                ReportedAt = report.ReportedAt,
                Status = report.Status,
                AdminNotes = report.AdminNotes,
                ReportedUser = new UserDto
                {
                    Id = reportedUser.Id,
                    UserName = reportedUser.UserName!,
                    DisplayName = reportedUser.DisplayName,
                    Avatar = reportedUser.Avatar
                },
                Reporter = new UserDto
                {
                    Id = reporter.Id,
                    UserName = reporter.UserName!,
                    DisplayName = reporter.DisplayName,
                    Avatar = reporter.Avatar
                }
            };
        }
    }
}
