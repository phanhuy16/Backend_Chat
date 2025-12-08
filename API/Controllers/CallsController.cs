using Core.DTOs.Call;
using Core.Interfaces.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CallsController : ControllerBase
    {
        private readonly ICallService _callService;
        private readonly ILogger<CallsController> _logger;

        public CallsController(ICallService callService, ILogger<CallsController> logger)
        {
            _callService = callService;
            _logger = logger;
        }

        // Helper để lấy User ID từ JWT token
        private int GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("id")?.Value;

            if (!int.TryParse(userIdClaim, out var userId))
            {
                throw new UnauthorizedAccessException("Invalid user ID");
            }

            return userId;
        }

        /// <summary>
        /// Bắt đầu cuộc gọi
        /// POST: api/call/initiate
        /// </summary>
        [HttpPost("initiate")]
        public async Task<IActionResult> InitiateCall([FromBody] InitiateCallRequest request)
        {
            try
            {
                var initiatorId = GetUserId();

                // Validate request
                if (request.ReceiverId <= 0 || request.ConversationId <= 0)
                {
                    return BadRequest(new { message = "Invalid receiver or conversation ID" });
                }

                if (string.IsNullOrEmpty(request.CallType))
                {
                    return BadRequest(new { message = "Call type is required" });
                }

                // Initiate call via service
                var callId = await _callService.InitiateCallAsync(
                    initiatorId,
                    request.ReceiverId,
                    request.ConversationId,
                    request.CallType
                );

                _logger.LogInformation($"Call initiated: {callId}");

                return Ok(new
                {
                    callId = callId,
                    initiatorId = initiatorId,
                    receiverId = request.ReceiverId,
                    message = "Call initiated successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating call");
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Trả lời cuộc gọi
        /// POST: api/call/{callId}/answer
        /// </summary>
        [HttpPost("{callId}/answer")]
        public async Task<IActionResult> AnswerCall(string callId)
        {
            try
            {
                var receiverId = GetUserId();

                var result = await _callService.AnswerCallAsync(callId, receiverId);

                if (!result)
                {
                    return BadRequest(new { message = "Failed to answer call" });
                }

                _logger.LogInformation($"Call answered: {callId}");

                return Ok(new { message = "Call answered successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error answering call {callId}");
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Từ chối cuộc gọi
        /// POST: api/call/{callId}/reject
        /// </summary>
        [HttpPost("{callId}/reject")]
        public async Task<IActionResult> RejectCall(string callId)
        {
            try
            {
                var result = await _callService.RejectCallAsync(callId, GetUserId());

                if (!result)
                {
                    return BadRequest(new { message = "Failed to reject call" });
                }

                _logger.LogInformation($"Call rejected: {callId}");

                return Ok(new { message = "Call rejected successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error rejecting call {callId}");
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Kết thúc cuộc gọi
        /// POST: api/call/{callId}/end
        /// </summary>
        [HttpPost("{callId}/end")]
        public async Task<IActionResult> EndCall(string callId, [FromBody] CallEndRequest request)
        {
            try
            {
                var result = await _callService.EndCallAsync(callId, request.Duration);

                if (!result)
                {
                    return BadRequest(new { message = "Failed to end call" });
                }

                _logger.LogInformation($"Call ended: {callId}, Duration: {request.Duration}s");

                return Ok(new { message = "Call ended successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error ending call {callId}");
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Cập nhật trạng thái media (mute/unmute, camera on/off)
        /// PUT: api/call/{callId}/media-state
        /// </summary>
        [HttpPut("{callId}/media-state")]
        public async Task<IActionResult> UpdateMediaState(string callId, [FromBody] UpdateMediaStateRequest request)
        {
            try
            {
                var userId = GetUserId();

                var result = await _callService.UpdateMediaStateAsync(
                    callId,
                    userId,
                    request.IsMuted,
                    request.IsVideoOff
                );

                if (!result)
                {
                    return BadRequest(new { message = "Failed to update media state" });
                }

                _logger.LogInformation($"Media state updated for call {callId}");

                return Ok(new { message = "Media state updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating media state for call {callId}");
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Lấy lịch sử cuộc gọi của user
        /// GET: api/call/history/{userId}
        /// </summary>
        [HttpGet("history/{userId}")]
        public async Task<IActionResult> GetCallHistory(int userId)
        {
            try
            {
                // Kiểm tra user chỉ có thể xem lịch sử của chính mình
                var currentUserId = GetUserId();
                if (currentUserId != userId)
                {
                    return Forbid("You can only view your own call history");
                }

                var callHistory = await _callService.GetCallHistoryAsync(userId);

                return Ok(new
                {
                    count = callHistory.Count,
                    calls = callHistory
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching call history for user {userId}");
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Lấy lịch sử cuộc gọi trong một conversation
        /// GET: api/call/conversation/{conversationId}
        /// </summary>
        [HttpGet("conversation/{conversationId}")]
        public async Task<IActionResult> GetConversationCalls(int conversationId)
        {
            try
            {
                var callHistory = await _callService.GetConversationCallsAsync(conversationId);

                return Ok(new
                {
                    count = callHistory.Count,
                    calls = callHistory
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching calls for conversation {conversationId}");
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Lấy call types enum
        /// GET: api/call/types
        /// </summary>
        [HttpGet("types")]
        [AllowAnonymous]
        public IActionResult GetCallTypes()
        {
            try
            {
                var callTypes = Enum.GetValues(typeof(Core.Enums.CallType))
                    .Cast<Core.Enums.CallType>()
                    .Select(ct => new { value = (int)ct, name = ct.ToString() })
                    .ToList();

                return Ok(callTypes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching call types");
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Lấy call statuses enum
        /// GET: api/call/statuses
        /// </summary>
        [HttpGet("statuses")]
        [AllowAnonymous]
        public IActionResult GetCallStatuses()
        {
            try
            {
                var callStatuses = Enum.GetValues(typeof(Core.Enums.CallStatus))
                    .Cast<Core.Enums.CallStatus>()
                    .Select(cs => new { value = (int)cs, name = cs.ToString() })
                    .ToList();

                return Ok(callStatuses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching call statuses");
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Health check endpoint
        /// GET: api/call/health
        /// </summary>
        [HttpGet("health")]
        [AllowAnonymous]
        public IActionResult HealthCheck()
        {
            return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
        }
    }

}
