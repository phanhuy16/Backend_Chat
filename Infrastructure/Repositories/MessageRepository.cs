using Core.Entities;
using Core.Interfaces.IRepositories;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Repositories
{
    public class MessageRepository : Repository<Message>, IMessageRepository
    {
        private readonly ILogger<MessageRepository> _logger;

        public MessageRepository(ChatAppDbContext context, ILogger<MessageRepository> logger)
            : base(context)
        {
            _logger = logger;
        }

        public async Task<IEnumerable<Message>> GetConversationMessagesAsync(int conversationId, int pageNumber, int pageSize)
        {
            try
            {
                _logger.LogInformation(
                    "Fetching messages for ConversationId={ConversationId}, Page={Page}, Size={Size}",
                    conversationId, pageNumber, pageSize);

                var messages = await _context.Messages
                    .Where(m => m.ConversationId == conversationId && !m.IsDeleted)
                    .Include(m => m.Sender)
                    .Include(m => m.Reactions)
                    .Include(m => m.Attachments)
                    .OrderByDescending(m => m.CreatedAt)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                _logger.LogInformation(
                    "Fetched {Count} messages for ConversationId={ConversationId}",
                    messages.Count, conversationId);

                return messages;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error fetching messages for ConversationId={ConversationId}, Page={Page}, Size={PageSize}",
                    conversationId, pageNumber, pageSize);

                throw;
            }
        }

        public async Task<Message> GetMessageWithReactionsAsync(int messageId)
        {
            try
            {
                _logger.LogInformation("Fetching message with reactions. MessageId={MessageId}", messageId);

                var message = await _context.Messages
                    .Include(m => m.Reactions)
                    .Include(m => m.Attachments)
                    .Include(m => m.Sender)
                    .FirstOrDefaultAsync(m => m.Id == messageId);

                if (message == null)
                {
                    _logger.LogWarning("MessageId={MessageId} not found", messageId);
                }

                return message!;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching message with reactions. MessageId={MessageId}", messageId);
                throw;
            }
        }
    }
}
