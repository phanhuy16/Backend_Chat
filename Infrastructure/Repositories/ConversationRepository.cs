
using Core.Entities;
using Core.Enums;
using Core.Interfaces.IRepositories;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Repositories
{
    public class ConversationRepository : Repository<Conversations>, IConversationRepository
    {
        private readonly ILogger<ConversationRepository> _logger;
        public ConversationRepository(ChatAppDbContext context, ILogger<ConversationRepository> logger) : base(context)
        {
            _logger = logger;
        }

        public async Task<Conversations> GetConversationWithMembersAsync(int conversationId)
        {
            try
            {
                _logger.LogInformation("Fetching conversation {ConversationId} including members and messages", conversationId);

                var conversation = await _context.Conversations
                         .Include(c => c.Members).ThenInclude(m => m.User)
                         .Include(c => c.Messages.Where(m => m.ScheduledAt == null || m.ScheduledAt <= DateTime.UtcNow).OrderByDescending(m => m.CreatedAt).Take(50))
                             .ThenInclude(m => m.Sender)
                         .Include(c => c.Messages)
                             .ThenInclude(m => m.Reactions)
                         .Include(c => c.Messages)
                             .ThenInclude(m => m.Attachments)
                         .FirstOrDefaultAsync(c => c.Id == conversationId);

                if (conversation == null)
                {
                    _logger.LogWarning("Conversation {ConversationId} not found", conversationId);
                }

                return conversation!;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching conversation {ConversationId}", conversationId);
                throw; // Re-throw the exception after logging it
            }
        }

        public async Task<Conversations> GetDirectConversationAsync(int userId1, int userId2)
        {
            _logger.LogInformation("Fetching direct conversation between {User1} and {User2}", userId1, userId2);

            try
            {
                var result = await _context.Conversations
                    .Include(c => c.Members)
                    .ThenInclude(m => m.User)
                    .FirstOrDefaultAsync(c => c.ConversationType == ConversationType.Direct &&
                        c.Members.Any(m => m.UserId == userId1) &&
                        c.Members.Any(m => m.UserId == userId2)) ?? null!;

                if (result == null)
                {
                    _logger.LogWarning("No direct conversation found between {User1} and {User2}", userId1, userId2);
                }
                return result!;
            }

            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching direct conversation between {User1} and {User2}", userId1, userId2);
                throw;
            }
        }

        public async Task<IEnumerable<Conversations>> GetUserConversationsAsync(int userId)
        {
            try
            {
                _logger.LogInformation("Fetching conversations for user {UserId}", userId);
                var conversations = await _context.Conversations
                    .Include(c => c.Members)
                    .ThenInclude(m => m.User)
                    .Where(c => c.Members
                    .Any(m => m.UserId == userId && !m.IsArchived))
                    .OrderByDescending(c => c.UpdatedAt).
                    ToListAsync();

                if (!conversations.Any())
                {
                    _logger.LogWarning("No conversations found for user {UserId}", userId);
                }

                foreach (var conversation in conversations)
                {
                    var latestMessages = await _context.Messages
                        .Where(m => m.ConversationId == conversation.Id && (m.ScheduledAt == null || m.ScheduledAt <= DateTime.UtcNow))
                        .OrderByDescending(m => m.CreatedAt)
                        .Take(1)
                        .Include(m => m.Sender)
                        .Include(m => m.Reactions)
                        .Include(m => m.Attachments)
                        .ToListAsync();

                    conversation.Messages = latestMessages;
                }

                return conversations;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching conversations for user {UserId}", userId);
                throw;
            }
        }

        public async Task<IEnumerable<Conversations>> GetUserArchivedConversationsAsync(int userId)
        {
            try
            {
                _logger.LogInformation("Fetching archived conversations for user {UserId}", userId);
                var conversations = await _context.Conversations
                    .Include(c => c.Members)
                    .ThenInclude(m => m.User)
                    .Where(c => c.Members
                    .Any(m => m.UserId == userId && m.IsArchived))
                    .OrderByDescending(c => c.UpdatedAt).
                    ToListAsync();

                if (!conversations.Any())
                {
                    _logger.LogWarning("No archived conversations found for user {UserId}", userId);
                }

                foreach (var conversation in conversations)
                {
                    var latestMessages = await _context.Messages
                        .Where(m => m.ConversationId == conversation.Id && (m.ScheduledAt == null || m.ScheduledAt <= DateTime.UtcNow))
                        .OrderByDescending(m => m.CreatedAt)
                        .Take(1)
                        .Include(m => m.Sender)
                        .Include(m => m.Reactions)
                        .Include(m => m.Attachments)
                        .ToListAsync();

                    conversation.Messages = latestMessages;
                }

                return conversations;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching archived conversations for user {UserId}", userId);
                throw;
            }
        }

        public async Task<IEnumerable<Message>> GetConversationMessagesAsync(
          int conversationId,
          int pageNumber = 1,
          int pageSize = 50)
        {
            try
            {
                _logger.LogInformation("Fetching messages for conversation {ConversationId}, Page: {PageNumber}, Size: {PageSize}",
                    conversationId, pageNumber, pageSize);
                var result = await _context.Messages
                      .Where(m => m.ConversationId == conversationId)
                      .OrderByDescending(m => m.CreatedAt)
                      .Skip((pageNumber - 1) * pageSize)
                      .Take(pageSize)
                      .Include(m => m.Sender)
                      .Include(m => m.Reactions)
                      .Include(m => m.Attachments)
                      .ToListAsync();

                if (!result.Any())
                {
                    _logger.LogWarning("No messages found for conversation {ConversationId}", conversationId);
                }

                return result;
            }
            catch
            (Exception ex)
            {
                _logger.LogError(ex, "Error fetching messages for conversation {ConversationId}", conversationId);
                throw;
            }
        }

        public async Task<ConversationMembers> GetConversationMemberAsync(int conversationId, int userId)
        {
            return await _context.ConversationMembers
                .FirstOrDefaultAsync(m => m.ConversationId == conversationId && m.UserId == userId) ?? null!;
        }

        public async Task<IEnumerable<Message>> GetMessagesWithContentAsync(int conversationId, string contentPart)
        {
            return await _context.Messages
                .Where(m => m.ConversationId == conversationId && 
                            !m.IsDeleted && 
                            m.Content != null && 
                            m.Content.ToLower().Contains(contentPart.ToLower()))
                .Include(m => m.Sender)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();
        }
    }
}
