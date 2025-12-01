

using Core.DTOs.Messages;
using Core.Enums;

namespace Core.Interfaces.IServices
{
    public interface IMessageService
    {
        Task<MessageDto> SendMessageAsync(int conversationId, int senderId, string content, MessageType messageType);
        Task<IEnumerable<MessageDto>> GetConversationMessagesAsync(int conversationId, int pageNumber, int pageSize);
        Task<MessageDto> EditMessageAsync(int messageId, string newContent);
        Task DeleteMessageAsync(int messageId);
        Task<ReactionDto> AddReactionAsync(int messageId, int userId, string emoji);
        Task<bool> RemoveReactionAsync(int reactionId);
        Task<IEnumerable<ReactionDto>> GetMessageReactionsAsync(int messageId);
    }
}
