using API.Hubs;
using Core.DTOs.ChatHub;
using Core.Entities;
using Core.Interfaces.IServices;
using Infrastructure.Data;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace API.Services
{
    public class ScheduledMessageWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ScheduledMessageWorker> _logger;
        private readonly IHubContext<ChatHub> _hubContext;

        public ScheduledMessageWorker(
            IServiceProvider serviceProvider,
            ILogger<ScheduledMessageWorker> logger,
            IHubContext<ChatHub> hubContext)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _hubContext = hubContext;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ScheduledMessageWorker started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessScheduledMessagesAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred in ScheduledMessageWorker.");
                }

                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }

        private async Task ProcessScheduledMessagesAsync(CancellationToken stoppingToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ChatAppDbContext>();
            var messageService = scope.ServiceProvider.GetRequiredService<IMessageService>();

            var now = DateTime.UtcNow;
            var scheduledMessages = await context.Messages
                .Include(m => m.Sender)
                .Include(m => m.Conversation)
                .Include(m => m.Attachments)
                .Include(m => m.Poll)
                    .ThenInclude(p => p!.Options)
                        .ThenInclude(o => o.Votes)
                            .ThenInclude(v => v!.User)
                .Where(m => m.ScheduledAt != null && m.ScheduledAt <= now)
                .ToListAsync(stoppingToken);

            if (scheduledMessages.Any())
            {
                _logger.LogInformation("Processing {Count} scheduled messages.", scheduledMessages.Count);

                foreach (var message in scheduledMessages)
                {
                    // Map to MessageDto
                    var messageDto = await messageService.MapToMessageDto(message);
                    
                    // Clear ScheduledAt to mark it as sent/released
                    message.ScheduledAt = null;
                    
                    // Map to ChatMessage for SignalR
                    var chatMessage = new ChatMessage
                    {
                        MessageId = messageDto.Id,
                        ConversationId = messageDto.ConversationId,
                        SenderId = messageDto.SenderId,
                        SenderName = messageDto.Sender?.DisplayName ?? string.Empty,
                        SenderAvatar = messageDto.Sender?.Avatar ?? string.Empty,
                        Content = messageDto.Content,
                        MessageType = messageDto.MessageType,
                        CreatedAt = messageDto.CreatedAt,
                        ParentMessageId = messageDto.ParentMessageId,
                        ParentMessage = messageDto.ParentMessage != null ? new ChatMessage
                        {
                            MessageId = messageDto.ParentMessage.Id,
                            Content = messageDto.ParentMessage.Content,
                            SenderName = messageDto.ParentMessage.Sender.DisplayName
                        } : null,
                        Poll = messageDto.Poll
                    };

                    // Broadcast via SignalR
                    await _hubContext.Clients.Group($"conversation_{message.ConversationId}")
                        .SendAsync("ReceiveMessage", chatMessage, cancellationToken: stoppingToken);
                }

                await context.SaveChangesAsync(stoppingToken);
            }
        }
    }
}
