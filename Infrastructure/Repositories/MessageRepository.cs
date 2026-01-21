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

        public async Task<IEnumerable<Message>> GetConversationMessagesAsync(int conversationId, int userId, int pageNumber, int pageSize)
        {
            try
            {
                _logger.LogInformation(
                    "Fetching messages for ConversationId={ConversationId}, Page={Page}, Size={Size}",
                    conversationId, pageNumber, pageSize);

                var messages = await _context.Messages
                    .Where(m => m.ConversationId == conversationId)
                    .Include(m => m.Sender)
                    .Include(m => m.Reactions)
                    .Include(m => m.Attachments)
                    .Include(m => m.DeletedForUsers)
                    .Include(m => m.ParentMessage)
                        .ThenInclude(p => p!.Sender)
                    .Include(m => m.Poll)
                        .ThenInclude(p => p!.Options)
                            .ThenInclude(o => o.Votes)
                                .ThenInclude(v => v!.User)
                    .Include(m => m.Mentions)
                        .ThenInclude(men => men.User)
                .Where(m => m.ConversationId == conversationId && (m.ScheduledAt == null || m.ScheduledAt <= DateTime.UtcNow))
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
                    .Include(m => m.ParentMessage)
                        .ThenInclude(p => p!.Sender)
                    .Include(m => m.Poll)
                        .ThenInclude(p => p!.Options)
                            .ThenInclude(o => o.Votes)
                                .ThenInclude(v => v!.User)
                    .Include(m => m.Mentions)
                        .ThenInclude(men => men.User)
                .FirstOrDefaultAsync(m => m.Id == messageId && (m.ScheduledAt == null || m.ScheduledAt <= DateTime.UtcNow));

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

        public async Task AddDeletedForUserAsync(MessageDeletedForUser deletedForUser)
        {
            try
            {
                await _context.MessageDeletedForUsers.AddAsync(deletedForUser);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding MessageDeletedForUser. MessageId={MessageId}, UserId={UserId}", 
                    deletedForUser.MessageId, deletedForUser.UserId);
                throw;
            }
        }

        public async Task AddReadStatusAsync(MessageReadStatus readStatus)
        {
            try
            {
                var existing = await _context.MessageReadStatuses
                    .FirstOrDefaultAsync(s => s.MessageId == readStatus.MessageId && s.UserId == readStatus.UserId);
                
                if (existing == null)
                {
                    await _context.MessageReadStatuses.AddAsync(readStatus);
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding MessageReadStatus. MessageId={MessageId}, UserId={UserId}", 
                    readStatus.MessageId, readStatus.UserId);
                throw;
            }
        }

        public async Task<IEnumerable<MessageReadStatus>> GetMessageReadStatusesAsync(int messageId)
        {
            return await _context.MessageReadStatuses
                .Where(s => s.MessageId == messageId)
                .Include(s => s.User)
                .ToListAsync();
        }

        public async Task<IEnumerable<Message>> SearchMessagesAsync(int conversationId, string query)
        {
            try
            {
                _logger.LogInformation("Searching messages in ConversationId={ConversationId} with Query={Query}", conversationId, query);

                return await _context.Messages
                    .Where(m => m.ConversationId == conversationId && 
                               !m.IsDeleted && 
                               m.Content != null && 
                               m.Content.ToLower().Contains(query.ToLower()) &&
                               (m.ScheduledAt == null || m.ScheduledAt <= DateTime.UtcNow))
                    .Include(m => m.Sender)
                    .Include(m => m.Reactions)
                    .Include(m => m.Attachments)
                    .Include(m => m.DeletedForUsers)
                    .Include(m => m.Mentions)
                        .ThenInclude(men => men.User)
                    .OrderByDescending(m => m.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching messages. ConversationId={ConversationId}, Query={Query}", conversationId, query);
                throw;
            }
        }

        public async Task<IEnumerable<Message>> GetPinnedMessagesAsync(int conversationId)
        {
            return await _context.Messages
                .Where(m => m.ConversationId == conversationId && m.IsPinned && !m.IsDeleted && (m.ScheduledAt == null || m.ScheduledAt <= DateTime.UtcNow))
                .Include(m => m.Sender)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Message>> SearchMessagesGloballyAsync(List<int> conversationIds, string query, int pageSize)
        {
            try
            {
                _logger.LogInformation("Searching messages globally with Query={Query}", query);

                return await _context.Messages
                    .Where(m => conversationIds.Contains(m.ConversationId) &&
                               !m.IsDeleted &&
                               m.Content != null &&
                               m.Content.ToLower().Contains(query.ToLower()) &&
                               (m.ScheduledAt == null || m.ScheduledAt <= DateTime.UtcNow))
                    .Include(m => m.Sender)
                    .Include(m => m.Conversation)
                        .ThenInclude(c => c.Members)
                            .ThenInclude(cm => cm.User)
                    .Include(m => m.Mentions)
                        .ThenInclude(men => men.User)
                    .OrderByDescending(m => m.CreatedAt)
                    .Take(pageSize)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching messages globally. Query={Query}", query);
                throw;
            }
        }

        public async Task<IEnumerable<Attachment>> SearchAttachmentsAsync(List<int> conversationIds, string query, int pageSize)
        {
            try
            {
                _logger.LogInformation("Searching attachments globally with Query={Query}", query);

                return await _context.Attachments
                    .Include(a => a.Message)
                        .ThenInclude(m => m.Conversation)
                            .ThenInclude(c => c.Members)
                                .ThenInclude(cm => cm.User)
                    .Where(a => a.Message != null &&
                               conversationIds.Contains(a.Message.ConversationId) &&
                               !a.Message.IsDeleted &&
                               a.FileName.ToLower().Contains(query.ToLower()))
                    .OrderByDescending(a => a.UploadedAt)
                    .Take(pageSize)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching attachments globally. Query={Query}", query);
                throw;
            }
        }

        public async Task AddMentionAsync(MessageMention mention)
        {
            try
            {
                await _context.MessageMentions.AddAsync(mention);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding MessageMention. MessageId={MessageId}, UserId={UserId}",
                    mention.MessageId, mention.UserId);
                throw;
            }
        }
    }
}
