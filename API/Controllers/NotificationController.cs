using Core.Entities;
using Core.Interfaces.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class NotificationController : ControllerBase
    {
        private readonly IPushNotificationService _pushService;

        public NotificationController(IPushNotificationService pushService)
        {
            _pushService = pushService;
        }

        [HttpGet("public-key")]
        public IActionResult GetPublicKey()
        {
            return Ok(new { publicKey = _pushService.GetPublicKey() });
        }

        [HttpPost("subscribe")]
        public async Task<IActionResult> Subscribe([FromBody] SubscriptionDto dto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (userId == 0) return Unauthorized();

            var sub = new PushSubscription
            {
                Endpoint = dto.Endpoint,
                P256DH = dto.Keys["p256dh"],
                Auth = dto.Keys["auth"]
            };

            await _pushService.SubscribeAsync(userId, sub);
            return Ok();
        }

        [HttpPost("unsubscribe")]
        public async Task<IActionResult> Unsubscribe([FromBody] UnsubscribeDto dto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (userId == 0) return Unauthorized();

            await _pushService.UnsubscribeAsync(userId, dto.Endpoint);
            return Ok();
        }

        [HttpPost("test")]
        public async Task<IActionResult> TestNotification()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            await _pushService.SendNotificationAsync(userId, "Test Notification", "This is a test message from ChatApp!");
            return Ok();
        }
    }

    public class SubscriptionDto
    {
        public string Endpoint { get; set; } = null!;
        public Dictionary<string, string> Keys { get; set; }= null!;
    }

    public class UnsubscribeDto
    {
        public string Endpoint { get; set; }= null!;
    }
}
