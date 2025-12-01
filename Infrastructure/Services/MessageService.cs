using Core.DTOs.Attachments;
using Core.DTOs.Messages;
using Core.DTOs.Users;
using Core.Entities;
using Core.Enums;
using Core.Interfaces.IRepositories;
using Core.Interfaces.IServices;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services
{
    public class MessageService : IMessageService
    {
        private readonly IMessageRepository _messageRepository;
        private readonly IReactionRepository _reactionRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<MessageService> _logger;

        public MessageService(IMessageRepository messageRepository,
                            IReactionRepository reactionRepository,
                            IUserRepository userRepository,
                            ILogger<MessageService> logger)
        {
            _messageRepository = messageRepository;
            _reactionRepository = reactionRepository;
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<ReactionDto> AddReactionAsync(int messageId, int userId, string emoji)
        {
            try
            {
                var message = await _messageRepository.GetByIdAsync(messageId);
                if (message == null)
                {
                    _logger.LogWarning("Message {MessageId} not found when adding reaction", messageId);
                    throw new Exception("Message not found");
                }

                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("User {UserId} not found when adding reaction", userId);
                    throw new Exception("User not found");
                }

                if (string.IsNullOrWhiteSpace(emoji))
                {
                    _logger.LogWarning("Empty emoji provided for reaction by user {UserId} on message {MessageId}", userId, messageId);
                    throw new Exception("Emoji cannot be empty");
                }

                var existingReaction = await _reactionRepository.GetReactionAsync(messageId, userId, emoji);
                if (existingReaction != null)
                {
                    return MapToReactionDto(existingReaction);
                }

                var reaction = new MessageReaction
                {
                    MessageId = messageId,
                    UserId = userId,
                    EmojiType = emoji,
                    CreatedAt = DateTime.UtcNow,
                };

                await _reactionRepository.AddAsync(reaction);

                message.UpdatedAt = DateTime.UtcNow;
                await _messageRepository.UpdateAsync(message);

                return MapToReactionDto(reaction);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding reaction for message {MessageId} by user {UserId}", messageId, userId);
                throw new Exception($"Error adding reaction: {ex.Message}");
            }
        }

        public async Task DeleteMessageAsync(int messageId)
        {
            try
            {
                var message = await _messageRepository.GetByIdAsync(messageId);
                if (message != null)
                {
                    message.IsDeleted = true;
                    message.UpdatedAt = DateTime.UtcNow;
                    await _messageRepository.UpdateAsync(message);
                }
                else
                {
                    _logger.LogWarning("Message {MessageId} not found for deletion", messageId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting message {MessageId}", messageId);
                throw;
            }
        }

        public async Task<MessageDto> EditMessageAsync(int messageId, string newContent)
        {
            try
            {
                var message = await _messageRepository.GetByIdAsync(messageId);
                if (message != null)
                {
                    message.Content = newContent;
                    message.UpdatedAt = DateTime.UtcNow;
                    await _messageRepository.UpdateAsync(message);
                    return MapToMessageDto(message);
                }
                else
                {
                    _logger.LogWarning("Message {MessageId} not found for edit", messageId);
                    return null!;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error editing message {MessageId}", messageId);
                throw;
            }
        }

        public async Task<IEnumerable<MessageDto>> GetConversationMessagesAsync(int conversationId, int pageNumber, int pageSize)
        {
            try
            {
                var messages = await _messageRepository.GetConversationMessagesAsync(conversationId, pageNumber, pageSize);
                return messages.Select(m => MapToMessageDto(m)).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting messages for conversation {ConversationId}", conversationId);
                throw;
            }
        }

        public async Task<IEnumerable<ReactionDto>> GetMessageReactionsAsync(int messageId)
        {
            try
            {
                var reactions = await _reactionRepository.GetMessageReactionsAsync(messageId);
                return reactions.Select(r => MapToReactionDto(r)).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting reactions for message {MessageId}", messageId);
                throw new Exception($"Error getting reactions: {ex.Message}");
            }
        }

        public async Task<bool> RemoveReactionAsync(int reactionId)
        {
            try
            {
                var reaction = await _reactionRepository.GetByIdAsync(reactionId);
                if (reaction == null)
                {
                    _logger.LogWarning("Reaction {ReactionId} not found for removal", reactionId);
                    return false;
                }

                var message = await _messageRepository.GetByIdAsync(reaction.MessageId);

                await _reactionRepository.DeleteAsync(reactionId);

                if (message != null)
                {
                    message.UpdatedAt = DateTime.UtcNow;
                    await _messageRepository.UpdateAsync(message);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing reaction {ReactionId}", reactionId);
                throw new Exception($"Error removing reaction: {ex.Message}");
            }
        }

        public async Task<MessageDto> SendMessageAsync(int conversationId, int senderId, string content, MessageType messageType)
        {
            try
            {
                var sender = await _userRepository.GetByIdAsync(senderId);

                var message = new Message
                {
                    ConversationId = conversationId,
                    SenderId = senderId,
                    Content = content,
                    MessageType = messageType,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                };

                await _messageRepository.AddAsync(message);

                var savedMessage = await _messageRepository.GetByIdAsync(message.Id);

                if (savedMessage.Sender == null)
                {
                    savedMessage.Sender = sender;
                }

                return MapToMessageDto(savedMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message for conversation {ConversationId} by user {SenderId}", conversationId, senderId);
                throw;
            }
        }

        private MessageDto MapToMessageDto(Message message)
        {
            return new MessageDto
            {
                Id = message.Id,
                ConversationId = message.ConversationId,
                SenderId = message.SenderId,
                Sender = new UserDto
                {
                    Id = message.Sender.Id,
                    UserName = message.Sender.UserName!,
                    DisplayName = message.Sender.DisplayName,
                    Avatar = message.Sender.Avatar,
                    Status = message.Sender.Status,
                },
                Content = message.Content,
                MessageType = message.MessageType,
                CreatedAt = message.CreatedAt,
                Reactions = message.Reactions.Select(r => new ReactionDto
                {
                    Id = r.Id,
                    UserId = r.UserId,
                    EmojiType = r.EmojiType,
                }).ToList(),
                Attachments = message.Attachments.Select(a => new AttachmentDto
                {
                    Id = a.Id,
                    FileName = a.FileName,
                    FileSize = a.FileSize,
                    FileUrl = a.FileUrl
                }).ToList()
            };
        }

        private ReactionDto MapToReactionDto(MessageReaction reaction)
        {
            return new ReactionDto
            {
                Id = reaction.Id,
                MessageId = reaction.MessageId,
                UserId = reaction.UserId,
                Username = reaction.User?.UserName ?? "",
                EmojiType = reaction.EmojiType,
                CreatedAt = reaction.CreatedAt
            };
        }
    }
}
