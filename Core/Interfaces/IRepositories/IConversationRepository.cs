

using Core.Entities;

namespace Core.Interfaces.IRepositories
{
    public interface IConversationRepository : IRepository<Conversations>
    {
        Task<Conversations> GetConversationWithMembersAsync(int conversationId);

        Task<Conversations> GetDirectConversationAsync(int userId1, int userId2);

        Task<IEnumerable<Conversations>> GetUserConversationsAsync(int userId);
        Task<IEnumerable<Conversations>> GetUserArchivedConversationsAsync(int userId);
        Task<IEnumerable<Message>> GetConversationMessagesAsync(
            int conversationId,
            int pageNumber = 1,
            int pageSize = 50);
        Task<ConversationMembers> GetConversationMemberAsync(int conversationId, int userId);
        Task<IEnumerable<Message>> GetMessagesWithContentAsync(int conversationId, string contentPart);
    }
}
