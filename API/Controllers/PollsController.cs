using System.Threading.Tasks;
using Core.DTOs;
using Core.Interfaces;
using Core.DTOs.Messages;
using Core.DTOs.ChatHub;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using API.Hubs;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PollsController : ControllerBase
    {
        private readonly IPollService _pollService;
        private readonly IHubContext<ChatHub> _hubContext;

        public PollsController(IPollService pollService, IHubContext<ChatHub> hubContext)
        {
            _pollService = pollService;
            _hubContext = hubContext;
        }

        [HttpPost]
        public async Task<ActionResult<PollDto>> CreatePoll([FromBody] CreatePollDto createPollDto)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
                var messageDto = await _pollService.CreatePollAsync(userId, createPollDto);
                
                // Broadcast PollCreated for specific poll listeners (if any)
                await _hubContext.Clients.Group($"conversation_{createPollDto.ConversationId}")
                    .SendAsync("PollCreated", messageDto.Poll);
                
                // Broadcast ReceiveMessage to update the chat stream in real-time
                var chatMessage = new ChatMessage
                {
                    MessageId = messageDto.Id,
                    ConversationId = messageDto.ConversationId,
                    SenderId = messageDto.SenderId,
                    SenderName = messageDto.Sender.DisplayName,
                    SenderAvatar = messageDto.Sender.Avatar,
                    Content = messageDto.Content,
                    MessageType = messageDto.MessageType,
                    CreatedAt = messageDto.CreatedAt,
                    Poll = messageDto.Poll
                };

                await _hubContext.Clients.Group($"conversation_{createPollDto.ConversationId}")
                    .SendAsync("ReceiveMessage", chatMessage);
                    
                return Ok(messageDto.Poll);
            }
            catch (System.Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{id}/vote")]
        public async Task<ActionResult<PollDto>> Vote(int id, [FromBody] int optionId)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
                var poll = await _pollService.VoteAsync(userId, id, optionId);
                
                await _hubContext.Clients.Group($"conversation_{poll.ConversationId}")
                    .SendAsync("PollUpdated", poll);

                return Ok(poll);
            }
            catch (System.Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        
        [HttpPost("{id}/vote/remove")]
        public async Task<ActionResult<PollDto>> RemoveVote(int id, [FromBody] int optionId)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
                var poll = await _pollService.RemoveVoteAsync(userId, id, optionId);
                
                await _hubContext.Clients.Group($"conversation_{poll.ConversationId}")
                    .SendAsync("PollUpdated", poll);

                return Ok(poll);
            }
            catch (System.Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<PollDto>> GetPoll(int id)
        {
            try
            {
                 var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
                 var poll = await _pollService.GetPollAsync(id, userId);
                 return Ok(poll);
            }
            catch (System.Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
