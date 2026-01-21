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
        Task<IEnumerable<Message>> SearchMessagesAsync(int conversationId, string query);
        Task<IEnumerable<Message>> GetPinnedMessagesAsync(int conversationId);
        Task<IEnumerable<Message>> SearchMessagesGloballyAsync(List<int> conversationIds, string query, int pageSize);
        Task<IEnumerable<Attachment>> SearchAttachmentsAsync(List<int> conversationIds, string query, int pageSize);
        Task AddMentionAsync(MessageMention mention);
    }
}
