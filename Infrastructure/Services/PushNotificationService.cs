using Core.Interfaces.IServices;
using Infrastructure.Data;
using Lib.Net.Http.WebPush;
using Lib.Net.Http.WebPush.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Services
{
    public class PushNotificationService : IPushNotificationService
    {
        private readonly ChatAppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly PushServiceClient _pushClient;

        public PushNotificationService(ChatAppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
            _pushClient = new PushServiceClient
            {
                DefaultAuthentication = new VapidAuthentication(
                    _configuration["Vapid:PublicKey"],
                    _configuration["Vapid:PrivateKey"])
                {
                    Subject = "mailto:admin@example.com"
                }
            };
        }

        public string GetPublicKey()
        {
            return _configuration["Vapid:PublicKey"]!;
        }

        public async Task SendNotificationAsync(int userId, string title, string message, string? url = null)
        {
            var subscriptions = await _context.PushSubscriptions
                .Where(s => s.UserId == userId)
                .ToListAsync();

            foreach (var sub in subscriptions)
            {
                var pushSubscription = new Lib.Net.Http.WebPush.PushSubscription
                {
                    Endpoint = sub.Endpoint,
                    Keys = new Dictionary<string, string>
                    {
                        { "p256dh", sub.P256DH! },
                        { "auth", sub.Auth! }
                    }
                };

                var notification = new PushMessage(message)
                {
                    Topic = title,
                    Urgency = PushMessageUrgency.High,
                };

                // Note: The library sends the payload as string/json usually. 
                // We'll wrap it in a simple JSON structure
                var payload = System.Text.Json.JsonSerializer.Serialize(new
                {
                    title,
                    message,
                    url = url ?? "/",
                    icon = "/logo192.png"
                });

                notification.Content = payload;

                try
                {
                    await _pushClient.RequestPushMessageDeliveryAsync(pushSubscription, notification);
                }
                catch (Exception ex)
                {
                    // If 410 Gone, remove subscription
                    if (ex.Message.Contains("410") || ex.Message.Contains("Gone"))
                    {
                        _context.PushSubscriptions.Remove(sub);
                    }
                    Console.WriteLine($"Error sending push: {ex.Message}");
                }
            }
            await _context.SaveChangesAsync();
        }

        public async Task SendNotificationToAllAsync(string title, string message, string? url = null)
        {
            var userIds = await _context.Users.Select(u => u.Id).ToListAsync();
            foreach (var userId in userIds)
            {
                await SendNotificationAsync(userId, title, message, url);
            }
        }

        public async Task SubscribeAsync(int userId, Core.Entities.PushSubscription subscription)
        {
            // Check if endpoint exists
            var existing = await _context.PushSubscriptions
                .FirstOrDefaultAsync(s => s.Endpoint == subscription.Endpoint);

            if (existing != null)
            {
                existing.UserId = userId; // Update user if changed
                existing.P256DH = subscription.P256DH;
                existing.Auth = subscription.Auth;
                _context.PushSubscriptions.Update(existing);
            }
            else
            {
                subscription.UserId = userId;
                await _context.PushSubscriptions.AddAsync(subscription);
            }
            await _context.SaveChangesAsync();
        }

        public async Task UnsubscribeAsync(int userId, string endpoint)
        {
            var sub = await _context.PushSubscriptions
                .FirstOrDefaultAsync(s => s.Endpoint == endpoint && s.UserId == userId);

            if (sub != null)
            {
                _context.PushSubscriptions.Remove(sub);
                await _context.SaveChangesAsync();
            }
        }
    }
}
