using API.Hubs;
using Core.Interfaces.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FriendsController : ControllerBase
    {
        private readonly IFriendService _friendService;
        private readonly IUserService _userService;
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly ILogger<FriendsController> _logger;

        public FriendsController(
            IFriendService friendService, 
            IUserService userService,
            IHubContext<ChatHub> hubContext,
            ILogger<FriendsController> logger)
        {
            _friendService = friendService;
            _userService = userService;
            _hubContext = hubContext;
            _logger = logger;
        }

        /// <summary>
        /// Send friend request
        /// </summary>
        [HttpPost("request/{targetUserId}")]
        [Authorize]
        public async Task<IActionResult> SendFriendRequest(int targetUserId)
        {
            var userId = GetCurrentUserId();
            if (userId <= 0)
                return Unauthorized("User not found");

            if (userId == targetUserId)
                return BadRequest("Cannot add yourself as friend");

            try
            {
                var result = await _friendService.SendFriendRequestAsync(userId, targetUserId);
                if (!result)
                    return BadRequest("Failed to send friend request");

                _logger.LogInformation($"Friend request sent from {userId} to {targetUserId}");

                // Send SignalR notification to receiver
                try
                {
                    var sender = await _userService.GetUserByIdAsync(userId);
                    if (sender != null)
                    {
                        await _hubContext.Clients.User(targetUserId.ToString())
                            .SendAsync("FriendRequestReceived", new
                            {
                                SenderId = userId,
                                SenderName = sender.DisplayName ?? sender.UserName,
                                SenderAvatar = sender.Avatar ?? "",
                                Timestamp = DateTime.UtcNow
                            });
                        _logger.LogInformation($"SignalR notification sent to user {targetUserId}");
                    }
                }
                catch (Exception notifyEx)
                {
                    _logger.LogWarning(notifyEx, $"Failed to send SignalR notification to user {targetUserId}");
                    // Don't fail the request if notification fails
                }

                return Ok(new { message = "Friend request sent successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending friend request from {userId} to {targetUserId}");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// Accept friend request
        /// </summary>
        [HttpPost("accept/{requestId}")]
        [Authorize]
        public async Task<IActionResult> AcceptFriendRequest(int requestId)
        {
            var userId = GetCurrentUserId();
            if (userId <= 0)
                return Unauthorized("User not found");

            try
            {
                var result = await _friendService.AcceptFriendRequestAsync(requestId, userId);
                if (!result)
                    return BadRequest("Failed to accept friend request");

                _logger.LogInformation($"Friend request {requestId} accepted by user {userId}");
                return Ok(new { message = "Friend request accepted" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error accepting friend request {requestId}");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// Reject friend request
        /// </summary>
        [HttpPost("reject/{requestId}")]
        [Authorize]
        public async Task<IActionResult> RejectFriendRequest(int requestId)
        {
            var userId = GetCurrentUserId();
            if (userId <= 0)
                return Unauthorized("User not found");

            try
            {
                var result = await _friendService.RejectFriendRequestAsync(requestId, userId);
                if (!result)
                    return BadRequest("Failed to reject friend request");

                _logger.LogInformation($"Friend request {requestId} rejected by user {userId}");
                return Ok(new { message = "Friend request rejected" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error rejecting friend request {requestId}");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// Get pending friend requests
        /// </summary>
        [HttpGet("requests/pending")]
        [Authorize]
        public async Task<IActionResult> GetPendingRequests()
        {
            var userId = GetCurrentUserId();
            if (userId <= 0)
                return Unauthorized("User not found");

            try
            {
                var requests = await _friendService.GetPendingRequestsAsync(userId);
                return Ok(requests);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching pending requests for user {userId}");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// Get sent friend requests
        /// </summary>
        [HttpGet("requests/sent")]
        [Authorize]
        public async Task<IActionResult> GetSentRequests()
        {
            var userId = GetCurrentUserId();
            if (userId <= 0)
                return Unauthorized("User not found");

            try
            {
                var requests = await _friendService.GetSentRequestsAsync(userId);
                return Ok(requests);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching sent requests for user {userId}");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// Get user's friends list
        /// </summary>
        [HttpGet("list")]
        [Authorize]
        public async Task<IActionResult> GetFriends()
        {
            var userId = GetCurrentUserId();
            if (userId <= 0)
                return Unauthorized("User not found");

            try
            {
                var friends = await _friendService.GetFriendsAsync(userId);
                return Ok(friends);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching friends for user {userId}");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// Remove friend
        /// </summary>
        [HttpDelete("remove/{friendId}")]
        [Authorize]
        public async Task<IActionResult> RemoveFriend(int friendId)
        {
            var userId = GetCurrentUserId();
            if (userId <= 0)
                return Unauthorized("User not found");

            try
            {
                var result = await _friendService.RemoveFriendAsync(userId, friendId);
                if (!result)
                    return BadRequest("Failed to remove friend");

                _logger.LogInformation($"User {userId} removed friend {friendId}");
                return Ok(new { message = "Friend removed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error removing friend {friendId}");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// Check if users are friends
        /// </summary>
        [HttpGet("check/{friendId}")]
        [Authorize]
        public async Task<IActionResult> CheckFriendship(int friendId)
        {
            var userId = GetCurrentUserId();
            if (userId <= 0)
                return Unauthorized("User not found");

            try
            {
                var isFriend = await _friendService.AreFriendsAsync(userId, friendId);
                return Ok(new { isFriend });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking friendship between {userId} and {friendId}");
                return StatusCode(500, new { message = ex.Message });
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
