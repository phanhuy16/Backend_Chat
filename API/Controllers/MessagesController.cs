using Core.DTOs.Messages;
using Core.Enums;
using Core.Interfaces.IServices;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MessagesController : ControllerBase
    {
        private readonly IMessageService _messageService;
        private readonly ILogger<MessagesController> _logger;

        public MessagesController(IMessageService messageService, ILogger<MessagesController> logger)
        {
            _messageService = messageService;
            _logger = logger;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetMessage(int id)
        {
            try
            {
                var message = await _messageService.GetMessageReactionsAsync(id);

                if (message == null)
                {
                    _logger.LogWarning("Message {MessageId} not found", id);
                    return NotFound();
                }

                return Ok(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching message {MessageId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
        {
            try
            {
                if (request.MessageType == MessageType.Text && string.IsNullOrWhiteSpace(request.Content))
                {
                    _logger.LogWarning("Attempt to send empty text message by user {SenderId}", request.SenderId);
                    return BadRequest("Message content is required for text messages");
                }

                var message = await _messageService.SendMessageAsync(
                    request.ConversationId,
                    request.SenderId,
                    request.Content,
                    request.MessageType,
                    request.ParentMessageId,
                    scheduledAt: null,
                    mentionedUserIds: request.MentionedUserIds,
                    selfDestructAfterSeconds: request.SelfDestructAfterSeconds);

                _logger.LogInformation("Message {MessageId} sent in conversation {ConversationId} by user {SenderId}",
                    message.Id, request.ConversationId, request.SenderId);

                return CreatedAtAction(nameof(GetMessage), new { id = message.Id }, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message in conversation {ConversationId} by user {SenderId}",
                    request.ConversationId, request.SenderId);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("conversation/{conversationId}")]
        public async Task<IActionResult> GetConversationMessages(int conversationId, [FromQuery] int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                if (pageNumber < 1 || pageSize < 1)
                {
                    _logger.LogWarning("Invalid paging parameters: pageNumber={PageNumber}, pageSize={PageSize}", pageNumber, pageSize);
                    return BadRequest("Page number and page size must be greater than 0");
                }

                var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                {
                    return Unauthorized();
                }

                var messages = await _messageService.GetConversationMessagesAsync(conversationId, userId, pageNumber, pageSize);
                return Ok(messages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching messages for conversation {ConversationId}", conversationId);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> EditMessage(int id, [FromBody] EditMessageRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Content))
                {
                    _logger.LogWarning("Attempt to edit message {MessageId} with empty content", id);
                    return BadRequest("Message content is required");
                }

                var message = await _messageService.EditMessageAsync(id, request.Content);

                if (message == null)
                {
                    _logger.LogWarning("Message {MessageId} not found for edit", id);
                    return NotFound();
                }

                _logger.LogInformation("Message {MessageId} edited", id);
                return Ok(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error editing message {MessageId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("{messageId}/reactions")]
        public async Task<IActionResult> AddReaction(int messageId, [FromBody] AddReactionRequest request)
        {
            try
            {
                await _messageService.AddReactionAsync(messageId, request.UserId, request.Emoji);

                _logger.LogInformation("Reaction added to message {MessageId} by user {UserId}", messageId, request.UserId);
                return Ok("Reaction added successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding reaction to message {MessageId} by user {UserId}", messageId, request.UserId);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpDelete("reactions/{reactionId}")]
        public async Task<IActionResult> RemoveReaction(int reactionId)
        {
            try
            {
                var success = await _messageService.RemoveReactionAsync(reactionId);

                if (!success)
                {
                    _logger.LogWarning("Reaction {ReactionId} not found for removal", reactionId);
                    return NotFound();
                }

                _logger.LogInformation("Reaction {ReactionId} removed successfully", reactionId);
                return Ok("Reaction removed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing reaction {ReactionId}", reactionId);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpDelete("{id}/me")]
        public async Task<IActionResult> DeleteMessageForMe(int id)
        {
            try
            {
                var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                {
                    return Unauthorized();
                }

                await _messageService.DeleteMessageForMeAsync(id, userId);
                return Ok("Message deleted for you");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting message {MessageId} for user", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchMessages([FromQuery] int conversationId, [FromQuery] string query)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query))
                {
                    return BadRequest("Query cannot be empty");
                }

                var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                {
                    return Unauthorized();
                }

                var messages = await _messageService.SearchMessagesAsync(conversationId, userId, query);
                return Ok(messages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching messages. ConversationId={ConversationId}, Query={Query}", conversationId, query);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("conversation/{conversationId}/pinned")]
        public async Task<IActionResult> GetPinnedMessages(int conversationId)
        {
            try
            {
                var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                {
                    return Unauthorized();
                }

                var messages = await _messageService.GetPinnedMessagesAsync(conversationId, userId);
                return Ok(messages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching pinned messages for conversation {ConversationId}", conversationId);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("{id}/readers")]
        public async Task<IActionResult> GetMessageReaders(int id)
        {
            try
            {
                var readers = await _messageService.GetMessageReadersAsync(id);
                return Ok(readers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching readers for message {MessageId}", id);
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
