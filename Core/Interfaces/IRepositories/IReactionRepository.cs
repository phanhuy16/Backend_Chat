using Core.Entities;
namespace Core.Interfaces.IRepositories
{
    public interface IReactionRepository : IRepository<MessageReaction>
    {
        Task<MessageReaction?> GetReactionAsync(int messageId, int userId, string emoji);
        Task<IEnumerable<MessageReaction>> GetMessageReactionsAsync(int messageId);
        Task<bool> ReactionExistsAsync(int messageId, int userId, string emoji);
    }
}
