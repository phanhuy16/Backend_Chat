using Core.Entities;
using Core.Interfaces.IRepositories;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Repositories
{
    public class ReactionRepository : Repository<MessageReaction>, IReactionRepository
    {
        private readonly ILogger<ReactionRepository> _logger;

        public ReactionRepository(ChatAppDbContext context, ILogger<ReactionRepository> logger)
            : base(context)
        {
            _logger = logger;
        }

        public async Task<IEnumerable<MessageReaction>> GetMessageReactionsAsync(int messageId)
        {
            try
            {
                return await _context.MessageReactions
                    .Where(x => x.MessageId == messageId)
                    .Include(r => r.User)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách reaction của message {MessageId}", messageId);
                return Enumerable.Empty<MessageReaction>();
            }
        }

        public async Task<MessageReaction?> GetReactionAsync(int messageId, int userId, string emoji)
        {
            try
            {
                return await _context.MessageReactions
                    .FirstOrDefaultAsync(r =>
                        r.MessageId == messageId &&
                        r.UserId == userId &&
                        r.EmojiType == emoji);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Lỗi khi tìm reaction của user {UserId} trong message {MessageId} với emoji {Emoji}",
                    userId, messageId, emoji);

                return null;
            }
        }

        public async Task<bool> ReactionExistsAsync(int messageId, int userId, string emoji)
        {
            try
            {
                var reaction = await GetReactionAsync(messageId, userId, emoji);
                return reaction != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Lỗi khi kiểm tra tồn tại reaction: message {MessageId}, user {UserId}, emoji {Emoji}",
                    messageId, userId, emoji);

                return false;
            }
        }
    }
}
