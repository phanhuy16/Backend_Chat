using Core.Entities;

namespace Core.Interfaces.IRepositories
{
    public interface IMessageRepository : IRepository<Message>
    {
        Task<IEnumerable<Message>> GetConversationMessagesAsync(int conversationId, int pageNumber, int pageSize);
        Task<Message> GetMessageWithReactionsAsync(int messageId);
    }
}
