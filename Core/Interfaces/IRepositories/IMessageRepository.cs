using Core.Entities;

namespace Core.Interfaces.IRepositories
{
    public interface IMessageRepository : IRepository<Message>
    {
        Task<IEnumerable<Message>> GetConversationMessagesAsync(int conversationId, int userId, int pageNumber, int pageSize);
        Task<Message> GetMessageWithReactionsAsync(int messageId);
        Task AddDeletedForUserAsync(MessageDeletedForUser deletedForUser);
        Task AddReadStatusAsync(MessageReadStatus readStatus);
        Task<IEnumerable<MessageReadStatus>> GetMessageReadStatusesAsync(int messageId);
    }
}
