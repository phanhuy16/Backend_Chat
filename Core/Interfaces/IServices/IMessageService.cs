

using Core.DTOs.Messages;
using Core.Enums;

namespace Core.Interfaces.IServices
{
    public interface IMessageService
    {
        Task<MessageDto> SendMessageAsync(int conversationId, int senderId, string? content, MessageType messageType, int? parentMessageId = null);
        Task<IEnumerable<MessageDto>> GetConversationMessagesAsync(int conversationId, int userId, int pageNumber, int pageSize);
        Task<MessageDto> EditMessageAsync(int messageId, string newContent);
        Task DeleteMessageAsync(int messageId);
        Task DeleteMessageForMeAsync(int messageId, int userId);
        Task<ReactionDto> AddReactionAsync(int messageId, int userId, string emoji);
        Task<bool> RemoveReactionAsync(int reactionId);
        Task<IEnumerable<ReactionDto>> GetMessageReactionsAsync(int messageId);
        Task<bool> TogglePinMessageAsync(int messageId);
        Task MarkAsReadAsync(int messageId, int userId);
        Task<MessageDto> ForwardMessageAsync(int messageId, int targetConversationId, int senderId);
    }
}
