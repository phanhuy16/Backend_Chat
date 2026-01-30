using API.Hubs;
using Core.Entities;
using Infrastructure.Data;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace API.Services
{
    /// <summary>
    /// Background worker that automatically deletes self-destructing messages after they expire
    /// </summary>
    public class SelfDestructMessageWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<SelfDestructMessageWorker> _logger;
        private readonly IHubContext<ChatHub> _hubContext;

        public SelfDestructMessageWorker(
            IServiceProvider serviceProvider,
            ILogger<SelfDestructMessageWorker> logger,
            IHubContext<ChatHub> hubContext)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _hubContext = hubContext;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("SelfDestructMessageWorker started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessExpiredMessagesAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred in SelfDestructMessageWorker.");
                }

                // Check every 5 seconds for expired messages
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        private async Task ProcessExpiredMessagesAsync(CancellationToken stoppingToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ChatAppDbContext>();

            var now = DateTime.UtcNow;
            
            // Find messages that have expired (ExpiresAt is set and has passed)
            var expiredMessages = await context.Messages
                .Where(m => m.ExpiresAt != null && m.ExpiresAt <= now && !m.IsDeleted)
                .ToListAsync(stoppingToken);

            if (expiredMessages.Any())
            {
                _logger.LogInformation("Processing {Count} expired self-destructing messages.", expiredMessages.Count);

                foreach (var message in expiredMessages)
                {
                    // Mark message as deleted
                    message.IsDeleted = true;
                    message.UpdatedAt = DateTime.UtcNow;
                    
                    // Notify clients via SignalR that the message has been deleted
                    await _hubContext.Clients.Group($"conversation_{message.ConversationId}")
                        .SendAsync("MessageDeleted", new { 
                            messageId = message.Id, 
                            conversationId = message.ConversationId,
                            reason = "self_destruct"
                        }, cancellationToken: stoppingToken);
                }

                await context.SaveChangesAsync(stoppingToken);
                _logger.LogInformation("Deleted {Count} expired self-destructing messages.", expiredMessages.Count);
            }
        }
    }
}
