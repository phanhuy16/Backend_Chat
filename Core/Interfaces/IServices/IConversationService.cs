

using Core.DTOs.Conversations;

namespace Core.Interfaces.IServices
{
    public interface IConversationService
    {
        Task<ConversationDto> GetConversationAsync(int conversationId);
        Task<IEnumerable<ConversationDto>> GetUserConversationsAsync(int userId);
        Task<ConversationDto> CreateDirectConversationAsync(int userId1, int userId2);
        Task<ConversationDto> CreateGroupConversationAsync(string groupName, int createdBy, List<int> memberIds);
        Task AddMemberToConversationAsync(int conversationId, int userId);
        Task RemoveMemberFromConversationAsync(int conversationId, int userId);
        Task TransferAdminRightsAsync(int conversationId, int fromUserId, int toUserId);
        Task DeleteGroupConversationAsync(int conversationId, int requestingUserId);
        Task LeaveConversationAsync(int conversationId, int userId);
        Task<bool> TogglePinConversationAsync(int conversationId, int userId);
    }
}
