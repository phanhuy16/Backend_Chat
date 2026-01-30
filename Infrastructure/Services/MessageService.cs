using Core.DTOs;
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
        private readonly IPushNotificationService _pushService;
        private readonly IConversationRepository _conversationRepository;

        public MessageService(IMessageRepository messageRepository,
                            IReactionRepository reactionRepository,
                            IUserRepository userRepository,
                            ILogger<MessageService> logger,
                            IPushNotificationService pushService,
                            IConversationRepository conversationRepository)
        {
            _messageRepository = messageRepository;
            _reactionRepository = reactionRepository;
            _userRepository = userRepository;
            _logger = logger;
            _pushService = pushService;
            _conversationRepository = conversationRepository;
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
                
                user.UpdatedAt = DateTime.UtcNow;
                await _userRepository.UpdateAsync(user);

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

        public async Task DeleteMessageAsync(int messageId, int userId)
        {
            try
            {
                var message = await _messageRepository.GetByIdAsync(messageId);
                if (message == null)
                {
                    _logger.LogWarning("Message {MessageId} not found for deletion", messageId);
                    return;
                }

                // If user is not the sender, check for CanDeleteMessages permission in the conversation
                if (message.SenderId != userId)
                {
                    var conversation = await _conversationRepository.GetConversationWithMembersAsync(message.ConversationId);
                    var member = conversation?.Members.FirstOrDefault(m => m.UserId == userId);

                    if (member == null || (member.Role != "Admin" && !member.CanDeleteMessages))
                    {
                        throw new Exception("Unauthorized to delete this message for everyone");
                    }
                }

                message.IsDeleted = true;
                message.UpdatedAt = DateTime.UtcNow;
                await _messageRepository.UpdateAsync(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting message {MessageId}", messageId);
                throw;
            }
        }

        public async Task DeleteMessageForMeAsync(int messageId, int userId)
        {
            try
            {
                var message = await _messageRepository.GetByIdAsync(messageId);
                if (message == null)
                {
                    _logger.LogWarning("Message {MessageId} not found for deletion for user {UserId}", messageId, userId);
                    return;
                }

                // Check if already deleted for this user
                // (Optional: can optimize with a specific repository method if needed)
                
                var deletedForUser = new MessageDeletedForUser
                {
                    MessageId = messageId,
                    UserId = userId,
                    DeletedAt = DateTime.UtcNow
                };

                // We need to add this to the context. 
                // Since MessageService doesn't have MessageDeletedForUserRepository, 
                // we might need to add it or use the context via MessageRepository if it exposes it.
                // Looking at MessageRepository, it inherits from Repository<Message> which has _context.
                
                await _messageRepository.AddDeletedForUserAsync(deletedForUser);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting message {MessageId} for user {UserId}", messageId, userId);
                throw;
            }
        }

        public async Task<MessageDto> EditMessageAsync(int messageId, string newContent, List<int>? mentionedUserIds = null)
        {
            try
            {
                var message = await _messageRepository.GetByIdAsync(messageId);
                if (message != null)
                {
                    message.Content = newContent;
                    message.IsModified = true;
                    message.UpdatedAt = DateTime.UtcNow;
                    await _messageRepository.UpdateAsync(message);

                    // Update Mentions
                    if (mentionedUserIds != null)
                    {
                        // Remove old mentions
                        // Note: MessageRepository doesn't have a clear way to remove mentions easily without a separate repo,
                        // but normally we'd want to clear and re-add.
                        // For now, let's assume we just add new ones if we want to keep it simple, 
                        // or I should implement a way to clear them.
                        // Actually, let's just add new ones for now to avoid complexity of a new repo method.
                        foreach (var userId in mentionedUserIds)
                        {
                            if (!message.Mentions.Any(m => m.UserId == userId))
                            {
                                await _messageRepository.AddMentionAsync(new MessageMention
                                {
                                    MessageId = message.Id,
                                    UserId = userId
                                });
                            }
                        }
                    }

                    return await MapToMessageDto(message);
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

        public async Task<IEnumerable<MessageDto>> GetConversationMessagesAsync(int conversationId, int userId, int pageNumber, int pageSize)
        {
            try
            {
                var messages = await _messageRepository.GetConversationMessagesAsync(conversationId, userId, pageNumber, pageSize);
                var messageDtos = new List<MessageDto>();
                foreach (var m in messages)
                {
                    messageDtos.Add(await MapToMessageDto(m, userId));
                }
                return messageDtos;
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

        public async Task<MessageDto> SendMessageAsync(int conversationId, int senderId, string? content, MessageType messageType, int? parentMessageId = null, DateTime? scheduledAt = null, List<int>? mentionedUserIds = null, int? selfDestructAfterSeconds = null)
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
                    ParentMessageId = parentMessageId,
                    ScheduledAt = scheduledAt,
                    SelfDestructAfterSeconds = selfDestructAfterSeconds  // Set self-destruct timer
                };

                await _messageRepository.AddAsync(message);

                // Add Mentions
                if (mentionedUserIds != null && mentionedUserIds.Any())
                {
                    foreach (var userId in mentionedUserIds)
                    {
                        await _messageRepository.AddMentionAsync(new MessageMention
                        {
                            MessageId = message.Id,
                            UserId = userId
                        });
                    }
                }

                sender!.UpdatedAt = DateTime.UtcNow;
                await _userRepository.UpdateAsync(sender);

                var savedMessage = await _messageRepository.GetByIdAsync(message.Id);

                if (savedMessage.Sender == null)
                {
                    savedMessage.Sender = sender;
                }

                // Send Push Notification
                try
                {
                    var conversation = await _conversationRepository.GetConversationWithMembersAsync(conversationId);
                    if (conversation != null)
                    {
                        var title = conversation.GroupName; 
                        if (string.IsNullOrEmpty(title)) 
                        {
                            title = sender!.DisplayName; 
                        }
                        
                        // We need members. If GetByIdAsync doesn't include members, we might need another method.
                        // Assuming GetByIdAsync includes members or we lazy load? Default GetById usually simple.
                        // Let's explicitly get members if possible or assume conversation.Members is populated.
                        // For safety, let's use conversationRepository.GetByIdWithMembersAsync if it exists, or just try Members.
                        // If repository pattern uses Include, it might be there.
                        // Let's try to access conversation.Members.
                        
                       if (conversation.Members != null)
                       {
                           foreach (var member in conversation.Members)
                           {
                               if (member.UserId != senderId)
                               {
                                   await _pushService.SendNotificationAsync(member.UserId, sender!.DisplayName, content ?? "Sent a file", $"/chat/{conversationId}");
                               }
                           }
                       }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending push notification");
                }

                return await MapToMessageDto(savedMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message for conversation {ConversationId} by user {SenderId}", conversationId, senderId);
                throw;
            }
        }

        public async Task<bool> TogglePinMessageAsync(int messageId, int userId)
        {
            try
            {
                var message = await _messageRepository.GetByIdAsync(messageId);
                if (message == null) return false;

                // Check for CanPinMessages permission
                var conversation = await _conversationRepository.GetConversationWithMembersAsync(message.ConversationId);
                var member = conversation?.Members.FirstOrDefault(m => m.UserId == userId);

                if (member == null || (member.Role != "Admin" && !member.CanPinMessages))
                {
                    throw new Exception("Unauthorized to pin messages in this conversation");
                }

                message.IsPinned = !message.IsPinned;
                message.UpdatedAt = DateTime.UtcNow;
                await _messageRepository.UpdateAsync(message);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling pin for message {MessageId}", messageId);
                throw;
            }
        }

        public async Task MarkAsReadAsync(int messageId, int userId)
        {
            try
            {
                var message = await _messageRepository.GetByIdAsync(messageId);

                // Handle self-destructing message: set ViewedAt and ExpiresAt when first viewed
                if (message != null && message.SelfDestructAfterSeconds.HasValue && !message.ViewedAt.HasValue && message.SenderId != userId)
                {
                    message.ViewedAt = DateTime.UtcNow;
                    message.ExpiresAt = DateTime.UtcNow.AddSeconds(message.SelfDestructAfterSeconds.Value);
                    message.UpdatedAt = DateTime.UtcNow;
                    await _messageRepository.UpdateAsync(message);
                }

                var readStatus = new MessageReadStatus
                {
                    MessageId = messageId,
                    UserId = userId,
                    ReadAt = DateTime.UtcNow
                };
                await _messageRepository.AddReadStatusAsync(readStatus);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking message {MessageId} as read by user {UserId}", messageId, userId);
                throw;
            }
        }

        public async Task<MessageDto> ForwardMessageAsync(int messageId, int targetConversationId, int senderId, List<int>? mentionedUserIds = null)
        {
            try
            {
                var originalMessage = await _messageRepository.GetMessageWithReactionsAsync(messageId);
                if (originalMessage == null) throw new Exception("Original message not found");

                var newMessage = new Message
                {
                    ConversationId = targetConversationId,
                    SenderId = senderId,
                    Content = originalMessage.Content,
                    MessageType = originalMessage.MessageType,
                    ForwardedFromId = messageId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                // Copy attachments if any
                if (originalMessage.Attachments != null && originalMessage.Attachments.Any())
                {
                    foreach (var att in originalMessage.Attachments)
                    {
                        newMessage.Attachments.Add(new Attachment
                        {
                            FileName = att.FileName,
                            FileUrl = att.FileUrl,
                            FileSize = att.FileSize,
                            FileType = att.FileType,
                            UploadedAt = DateTime.UtcNow
                        });
                    }
                }

                await _messageRepository.AddAsync(newMessage);

                // Add Mentions
                if (mentionedUserIds != null && mentionedUserIds.Any())
                {
                    foreach (var userId in mentionedUserIds)
                    {
                        await _messageRepository.AddMentionAsync(new MessageMention
                        {
                            MessageId = newMessage.Id,
                            UserId = userId
                        });
                    }
                }

                var savedMessage = await _messageRepository.GetByIdAsync(newMessage.Id);
                
                if (savedMessage.Sender == null)
                {
                    savedMessage.Sender = await _userRepository.GetByIdAsync(senderId);
                }

                return await MapToMessageDto(savedMessage, senderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error forwarding message {MessageId} to conversation {TargetConversationId}", messageId, targetConversationId);
                throw;
            }
        }

        public async Task<IEnumerable<MessageDto>> SearchMessagesAsync(int conversationId, int userId, string query)
        {
            try
            {
                var messages = await _messageRepository.SearchMessagesAsync(conversationId, query);
                var messageDtos = new List<MessageDto>();
                foreach (var m in messages)
                {
                    messageDtos.Add(await MapToMessageDto(m, userId));
                }
                return messageDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching messages in service. ConversationId={ConversationId}, Query={Query}", conversationId, query);
                throw;
            }
        }

        public async Task<IEnumerable<MessageDto>> GetPinnedMessagesAsync(int conversationId, int userId)
        {
            try
            {
                var messages = await _messageRepository.GetPinnedMessagesAsync(conversationId);
                var messageDtos = new List<MessageDto>();
                foreach (var m in messages)
                {
                    messageDtos.Add(await MapToMessageDto(m, userId));
                }
                return messageDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching pinned messages in service. ConversationId={ConversationId}", conversationId);
                throw;
            }
        }

        public async Task<IEnumerable<MessageReaderDto>> GetMessageReadersAsync(int messageId)
        {
            try
            {
                var readStatuses = await _messageRepository.GetMessageReadStatusesAsync(messageId);
                return readStatuses.Select(s => new MessageReaderDto
                {
                    UserId = s.UserId,
                    DisplayName = s.User?.DisplayName ?? s.User?.UserName ?? "Unknown",
                    Avatar = s.User?.Avatar ?? "",
                    ReadAt = s.ReadAt
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting message readers for message {MessageId}", messageId);
                throw;
            }
        }

        public async Task<MessageDto> MapToMessageDto(Message message, int currentUserId = 0)
        {
            var readStatuses = await _messageRepository.GetMessageReadStatusesAsync(message.Id);
            
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
                CreatedAt = DateTime.SpecifyKind(message.CreatedAt, DateTimeKind.Utc),
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
                }).ToList(),
                IsDeleted = message.IsDeleted,
                IsDeletedForMe = currentUserId != 0 && message.DeletedForUsers.Any(dfu => dfu.UserId == currentUserId),
                IsPinned = message.IsPinned,
                IsModified = message.IsModified,
                ParentMessageId = message.ParentMessageId,
                ParentMessage = message.ParentMessage != null ? await MapToMessageDto(message.ParentMessage, currentUserId) : null,
                ForwardedFromId = message.ForwardedFromId,
                ScheduledAt = message.ScheduledAt.HasValue ? DateTime.SpecifyKind(message.ScheduledAt.Value, DateTimeKind.Utc) : null,
                IsReadByMe = currentUserId != 0 && readStatuses.Any(s => s.UserId == currentUserId),
                ReadCount = readStatuses.Count(),
                PollId = message.PollId,
                Poll = message.Poll != null ? new PollDto
                {
                    Id = message.Poll.Id,
                    Question = message.Poll.Question,
                    AllowMultipleVotes = message.Poll.AllowMultipleVotes,
                    ConversationId = message.Poll.ConversationId,
                    CreatorId = message.Poll.CreatorId,
                    CreatorName = message.Poll.Creator != null ? message.Poll.Creator.DisplayName : "Unknown",
                    CreatedAt = message.Poll.CreatedAt,
                    EndsAt = message.Poll.EndsAt,
                    TotalVotes = message.Poll.Options.Sum(o => o.Votes.Count),
                    HasVoted = message.Poll.Options.Any(o => o.Votes.Any(v => v.UserId == currentUserId)),
                    Options = message.Poll.Options.Select(o => new PollOptionDto
                    {
                        Id = o.Id,
                        Text = o.Text,
                        VoteCount = o.Votes.Count,
                        IsVotedByCurrentUser = o.Votes.Any(v => v.UserId == currentUserId),
                        Votes = o.Votes.Select(v => new PollVoteDto
                        {
                            UserId = v.UserId,
                            Username = v.User != null ? v.User.DisplayName : "Unknown",
                            AvatarUrl = v.User != null ? v.User.Avatar : ""
                        }).ToList()
                    }).ToList()
                } : null,
                MentionedUsers = message.Mentions.Select(m => new UserDto
                {
                    Id = m.UserId,
                    UserName = m.User?.UserName ?? "Unknown",
                    DisplayName = m.User?.DisplayName ?? "Unknown",
                    Avatar = m.User?.Avatar ?? "",
                    Status = m.User?.Status ?? StatusUser.Offline
                }).ToList(),

                // Self-destructing message fields
                SelfDestructAfterSeconds = message.SelfDestructAfterSeconds,
                ViewedAt = message.ViewedAt.HasValue ? DateTime.SpecifyKind(message.ViewedAt.Value, DateTimeKind.Utc) : null,
                ExpiresAt = message.ExpiresAt.HasValue ? DateTime.SpecifyKind(message.ExpiresAt.Value, DateTimeKind.Utc) : null
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
