

using Core.DTOs.Messages;
using Core.Entities;
using Core.Enums;

namespace Core.Interfaces.IServices
{
    public interface IMessageService
    {
        Task<MessageDto> SendMessageAsync(int conversationId, int senderId, string? content, MessageType messageType, int? parentMessageId = null, DateTime? scheduledAt = null, List<int>? mentionedUserIds = null, int? selfDestructAfterSeconds = null);
        Task<IEnumerable<MessageDto>> GetConversationMessagesAsync(int conversationId, int userId, int pageNumber, int pageSize);
        Task<MessageDto> EditMessageAsync(int messageId, string newContent, List<int>? mentionedUserIds = null);
        Task DeleteMessageAsync(int messageId, int userId);
        Task DeleteMessageForMeAsync(int messageId, int userId);
        Task<ReactionDto> AddReactionAsync(int messageId, int userId, string emoji);
        Task<bool> RemoveReactionAsync(int reactionId);
        Task<IEnumerable<ReactionDto>> GetMessageReactionsAsync(int messageId);
        Task<bool> TogglePinMessageAsync(int messageId, int userId);
        Task MarkAsReadAsync(int messageId, int userId);
        Task<MessageDto> ForwardMessageAsync(int messageId, int targetConversationId, int senderId, List<int>? mentionedUserIds = null);
        Task<IEnumerable<MessageDto>> SearchMessagesAsync(int conversationId, int userId, string query);
        Task<IEnumerable<MessageDto>> GetPinnedMessagesAsync(int conversationId, int userId);
        Task<IEnumerable<MessageReaderDto>> GetMessageReadersAsync(int messageId);
        Task<MessageDto> MapToMessageDto(Message message, int currentUserId = 0);
    }
}
