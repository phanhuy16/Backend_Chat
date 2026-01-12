using API.Hubs;
using Core.DTOs.Conversations;
using Core.Interfaces.IServices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ConversationsController : ControllerBase
    {
        private readonly IConversationService _conversationService;
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly IAttachmentService _attachmentService;
        private readonly ILogger<ConversationsController> _logger;

        public ConversationsController(
            IConversationService conversationService,
            IHubContext<ChatHub> hubContext,
            IAttachmentService attachmentService,
            ILogger<ConversationsController> logger)
        {
            _conversationService = conversationService;
            _hubContext = hubContext;
            _attachmentService = attachmentService;
            _logger = logger;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetConversation(int id)
        {
            try
            {
                var conversation = await _conversationService.GetConversationAsync(id);

                if (conversation == null)
                {
                    _logger.LogWarning("Conversation {ConversationId} not found", id);
                    return NotFound("Conversation not found");
                }

                return Ok(conversation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching conversation {ConversationId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserConversations(int userId)
        {
            try
            {
                var conversations = await _conversationService.GetUserConversationsAsync(userId);
                return Ok(conversations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching conversations for user {UserId}", userId);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("direct")]
        public async Task<IActionResult> CreateDirectConversation([FromBody] CreateDirectConversationRequest request)
        {
            try
            {
                if (request.UserId1 == request.UserId2)
                {
                    _logger.LogWarning("Attempt to create direct conversation with self: {UserId}", request.UserId1);
                    return BadRequest("Cannot create conversation with yourself");
                }

                var conversation = await _conversationService.CreateDirectConversationAsync(request.UserId1, request.UserId2);

                await _hubContext.Clients.User(request.UserId1.ToString())
                    .SendAsync("NewConversationCreated", new { ConversationId = conversation.Id, Conversation = conversation });

                await _hubContext.Clients.User(request.UserId2.ToString())
                    .SendAsync("NewConversationCreated", new { ConversationId = conversation.Id, Conversation = conversation });

                _logger.LogInformation("Direct conversation {ConversationId} created between users {User1} and {User2}",
                    conversation.Id, request.UserId1, request.UserId2);

                return CreatedAtAction(nameof(GetConversation), new { id = conversation.Id }, conversation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating direct conversation between users {User1} and {User2}", request.UserId1, request.UserId2);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("group")]
        public async Task<IActionResult> CreateGroupConversation([FromBody] CreateGroupConversationRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.GroupName))
                {
                    _logger.LogWarning("Attempt to create group conversation without a name by user {UserId}", request.CreatedBy);
                    return BadRequest("Group name is required");
                }

                var conversation = await _conversationService.CreateGroupConversationAsync(request.GroupName, request.CreatedBy, request.MemberIds);

                foreach (var memberId in request.MemberIds)
                {
                    await _hubContext.Clients.User(memberId.ToString())
                        .SendAsync("NewConversationCreated", new { ConversationId = conversation.Id, Conversation = conversation });
                }

                _logger.LogInformation("Group conversation {ConversationId} created by user {CreatedBy}", conversation.Id, request.CreatedBy);

                return CreatedAtAction(nameof(GetConversation), new { id = conversation.Id }, conversation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating group conversation by user {CreatedBy}", request.CreatedBy);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("{conversationId}/members/{userId}")]
        public async Task<IActionResult> AddMember(int conversationId, int userId)
        {
            try
            {
                var conversation = await _conversationService.GetConversationAsync(conversationId);

                if (conversation == null)
                {
                    return NotFound("Conversation not found");
                }

                var requestingUser = GetCurrentUserId();
                var isMember = conversation.Members.Any(m => m.Id == requestingUser);

                if (!isMember)
                {
                    return Forbid("You must be a member of this conversation to add members");
                }

                await _conversationService.AddMemberToConversationAsync(conversationId, userId);

                await _hubContext.Clients.User(userId.ToString())
                    .SendAsync("AddedToConversation", new { ConversationId = conversationId });

                _logger.LogInformation("User {UserId} added to conversation {ConversationId}", userId, conversationId);

                return Ok("Member added successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding user {UserId} to conversation {ConversationId}", userId, conversationId);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpDelete("{conversationId}/members/{userId}")]
        public async Task<IActionResult> RemoveMember(int conversationId, int userId)
        {
            try
            {
                await _conversationService.RemoveMemberFromConversationAsync(conversationId, userId);

                await _hubContext.Clients.User(userId.ToString())
                    .SendAsync("RemovedFromConversation", new { ConversationId = conversationId });

                _logger.LogInformation("User {UserId} removed from conversation {ConversationId}", userId, conversationId);

                return Ok("Member removed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing user {UserId} from conversation {ConversationId}", userId, conversationId);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Transfer admin rights to another member
        /// </summary>
        [HttpPost("{conversationId}/transfer-admin")]
        public async Task<IActionResult> TransferAdminRights(int conversationId, [FromBody] TransferAdminRequest request)
        {
            try
            {
                if (request.FromUserId <= 0 || request.ToUserId <= 0)
                    return BadRequest("Invalid user IDs");

                await _conversationService.TransferAdminRightsAsync(conversationId, request.FromUserId, request.ToUserId);

                // Notify both users
                await _hubContext.Clients.User(request.FromUserId.ToString())
                    .SendAsync("AdminRightsTransferred", new { ConversationId = conversationId, NewAdmin = request.ToUserId });

                await _hubContext.Clients.User(request.ToUserId.ToString())
                    .SendAsync("BecameAdmin", new { ConversationId = conversationId });

                _logger.LogInformation("Admin rights transferred in conversation {ConversationId}", conversationId);
                return Ok("Admin rights transferred successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error transferring admin rights");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// Delete group conversation
        /// </summary>
        [HttpDelete("{conversationId}/delete")]
        public async Task<IActionResult> DeleteGroupConversation(int conversationId, [FromBody] DeleteGroupRequest request)
        {
            try
            {
                await _conversationService.DeleteGroupConversationAsync(conversationId, request.RequestingUserId);

                // Notify all members
                var conversation = await _conversationService.GetConversationAsync(conversationId);
                if (conversation != null)
                {
                    foreach (var member in conversation.Members)
                    {
                        await _hubContext.Clients.User(member.Id.ToString())
                            .SendAsync("ConversationDeleted", new { ConversationId = conversationId });
                    }
                }

                _logger.LogInformation("Group conversation {ConversationId} deleted", conversationId);
                return Ok("Group conversation deleted successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting conversation");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// Leave conversation
        /// </summary>
        [HttpPost("{conversationId}/leave")]
        public async Task<IActionResult> LeaveConversation(int conversationId, [FromBody] LeaveConversationRequest request)
        {
            try
            {
                await _conversationService.LeaveConversationAsync(conversationId, request.UserId);

                // Notify others
                await _hubContext.Clients.Group($"conversation_{conversationId}")
                    .SendAsync("UserLeftConversation", new { UserId = request.UserId, ConversationId = conversationId });

                _logger.LogInformation("User {UserId} left conversation {ConversationId}", request.UserId, conversationId);
                return Ok("Left conversation successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error leaving conversation");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// Get all attachments for a specific conversation
        /// </summary>
        [HttpGet("{id}/attachments")]
        public async Task<IActionResult> GetAttachments(int id)
        {
            try
            {
                var attachments = await _attachmentService.GetAttachmentsByConversationIdAsync(id);
                return Ok(attachments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching attachments for conversation {ConversationId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("{conversationId}/pin/{userId}")]
        public async Task<IActionResult> TogglePin(int conversationId, int userId)
        {
            try
            {
                var isPinned = await _conversationService.TogglePinConversationAsync(conversationId, userId);
                
                // Optional: Notify user via SignalR if they are connected on multiple devices
                await _hubContext.Clients.User(userId.ToString())
                    .SendAsync("ConversationPinStatusChanged", new { ConversationId = conversationId, IsPinned = isPinned });

                _logger.LogInformation("Conversation {ConversationId} pin status toggled to {IsPinned} for user {UserId}",
                    conversationId, isPinned, userId);

                return Ok(new { isPinned });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling pin status for conversation {ConversationId}", conversationId);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("{id}/links")]
        public async Task<IActionResult> GetConversationLinks(int id)
        {
            try
            {
                var links = await _conversationService.GetConversationLinksAsync(id);
                return Ok(links);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching links for conversation {ConversationId}", id);
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
