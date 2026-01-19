using Core.Interfaces.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SearchController : ControllerBase
    {
        private readonly ISearchService _searchService;
        private readonly ILogger<SearchController> _logger;

        public SearchController(ISearchService searchService, ILogger<SearchController> logger)
        {
            _searchService = searchService;
            _logger = logger;
        }

        /// <summary>
        /// Perform global search across messages, files, and users
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Search([FromQuery] string query, [FromQuery] int pageSize = 20)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId <= 0)
                    return Unauthorized("User not found");

                if (string.IsNullOrWhiteSpace(query))
                    return BadRequest("Search query cannot be empty");

                _logger.LogInformation("Global search request from user {UserId} with query: {Query}", userId, query);

                var results = await _searchService.SearchAsync(userId, query, pageSize);
                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing global search");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Search messages only
        /// </summary>
        [HttpGet("messages")]
        public async Task<IActionResult> SearchMessages([FromQuery] string query, [FromQuery] int pageSize = 20)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId <= 0)
                    return Unauthorized("User not found");

                if (string.IsNullOrWhiteSpace(query))
                    return BadRequest("Search query cannot be empty");

                var results = await _searchService.SearchMessagesAsync(userId, query, pageSize);
                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching messages");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Search files only
        /// </summary>
        [HttpGet("files")]
        public async Task<IActionResult> SearchFiles([FromQuery] string query, [FromQuery] int pageSize = 20)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId <= 0)
                    return Unauthorized("User not found");

                if (string.IsNullOrWhiteSpace(query))
                    return BadRequest("Search query cannot be empty");

                var results = await _searchService.SearchFilesAsync(userId, query, pageSize);
                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching files");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Search users only
        /// </summary>
        [HttpGet("users")]
        public async Task<IActionResult> SearchUsers([FromQuery] string query, [FromQuery] int pageSize = 20)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId <= 0)
                    return Unauthorized("User not found");

                if (string.IsNullOrWhiteSpace(query))
                    return BadRequest("Search query cannot be empty");

                var results = await _searchService.SearchUsersAsync(userId, query, pageSize);
                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching users");
                return StatusCode(500, "Internal server error");
            }
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                return userId;
            }
            return 0;
        }
    }
}
