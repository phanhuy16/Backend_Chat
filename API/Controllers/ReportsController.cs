using Core.DTOs.Reports;
using Core.Interfaces.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ReportsController : ControllerBase
    {
        private readonly IReportService _reportService;
        private readonly ILogger<ReportsController> _logger;

        public ReportsController(IReportService reportService, ILogger<ReportsController> logger)
        {
            _reportService = reportService;
            _logger = logger;
        }

        // POST: Create report
        [HttpPost]
        public async Task<IActionResult> CreateReport([FromBody] CreateReportDto reportDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var report = await _reportService.CreateReportAsync(reportDto);
                return Ok(new { message = "Report submitted successfully", data = report });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating report");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // GET: Get report by ID
        [HttpGet("{reportId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetReport(int reportId)
        {
            try
            {
                var report = await _reportService.GetReportAsync(reportId);
                return Ok(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting report {reportId}");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // GET: Get reports for user
        [HttpGet("user/{userId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetUserReports(int userId)
        {
            try
            {
                var reports = await _reportService.GetUserReportsAsync(userId);
                return Ok(reports);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting reports for user {userId}");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // GET: Get pending reports (admin)
        [HttpGet("pending")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetPendingReports()
        {
            try
            {
                var reports = await _reportService.GetPendingReportsAsync();
                return Ok(reports);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending reports");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // GET: Get reports by reporter
        [HttpGet("reporter/{reporterId}")]
        public async Task<IActionResult> GetReportsByReporter(int reporterId)
        {
            try
            {
                // Allow admin OR the user themselves
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var isAdmin = User.IsInRole("Admin");

                if (!isAdmin && currentUserId != reporterId)
                {
                    return Forbid();
                }

                var reports = await _reportService.GetReportsByReporterAsync(reporterId);
                return Ok(reports);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting reports by reporter {reporterId}");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // PUT: Update report status (admin only)
        [HttpPut("{reportId}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateReportStatus(int reportId, [FromBody] UpdateReportStatusDto updateDto)
        {
            try
            {
                await _reportService.UpdateReportStatusAsync(reportId, updateDto.Status, updateDto.AdminNotes);
                return Ok(new { message = "Report status updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating report {reportId} status");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // DELETE: Delete report (admin only)
        [HttpDelete("{reportId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteReport(int reportId)
        {
            try
            {
                await _reportService.DeleteReportAsync(reportId);
                return Ok(new { message = "Report deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting report {reportId}");
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }
}
