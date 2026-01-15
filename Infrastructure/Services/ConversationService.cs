using Core.DTOs.Conversations;
using Core.DTOs.Messages;
using Core.DTOs.Users;
using Core.Entities;
using Core.Enums;
using Core.Interfaces.IRepositories;
using Core.Interfaces.IServices;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace Infrastructure.Services
{
    public class ConversationService : IConversationService
    {
        private readonly IConversationRepository _conversationRepository;
        private readonly IUserRepository _userRepository;
        private readonly IRepository<ConversationMembers> _memberRepository;
        private readonly ILogger<ConversationService> _logger;

        public ConversationService(
            IConversationRepository conversationRepository,
            IUserRepository userRepository,
            IRepository<ConversationMembers> memberRepository,
            ILogger<ConversationService> logger)
        {
            _conversationRepository = conversationRepository;
            _userRepository = userRepository;
            _memberRepository = memberRepository;
            _logger = logger;
        }

        public async Task AddMemberToConversationAsync(int conversationId, int userId)
        {
            try
            {
                var conversation = await _conversationRepository.GetConversationWithMembersAsync(conversationId);

                if (conversation != null && !conversation.Members.Any(m => m.UserId == userId))
                {
                    var member = new ConversationMembers
                    {
                        ConversationId = conversationId,
                        UserId = userId,
                        Role = "Member"
                    };

                    conversation.Members.Add(member);
                    await _conversationRepository.UpdateAsync(conversation);
                    _logger.LogInformation($"Member {userId} added to conversation {conversationId}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error adding member: {ex.Message}");
                throw;
            }
        }

        public async Task<ConversationDto> CreateDirectConversationAsync(int userId1, int userId2)
        {
            try
            {
                // Validate users exist
                var user1 = await _userRepository.GetByIdAsync(userId1);
                var user2 = await _userRepository.GetByIdAsync(userId2);

                if (user1 == null)
                    throw new Exception($"User with ID {userId1} not found");

                if (user2 == null)
                    throw new Exception($"User with ID {userId2} not found");

                // Check if conversation already exists
                var existing = await _conversationRepository.GetDirectConversationAsync(userId1, userId2);
                if (existing != null)
                {
                    _logger.LogInformation($"Conversation already exists between users {userId1} and {userId2}");
                    return MapToConversationDto(existing, userId1);
                }

                _logger.LogInformation($"Creating direct conversation between users {userId1} and {userId2}");

                var conversation = new Conversations
                {
                    ConversationType = ConversationType.Direct,
                    CreatedBy = userId1,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                };

                // Step 1: Add conversation to database
                await _conversationRepository.AddAsync(conversation);
                _logger.LogInformation($"Conversation {conversation.Id} created");

                // Step 2: Create members
                var member1 = new ConversationMembers
                {
                    ConversationId = conversation.Id,
                    UserId = userId1,
                    Role = "Member",
                    JoinedAt = DateTime.UtcNow
                };

                var member2 = new ConversationMembers
                {
                    ConversationId = conversation.Id,
                    UserId = userId2,
                    Role = "Member",
                    JoinedAt = DateTime.UtcNow
                };

                // Step 3: Add members to conversation object
                conversation.Members.Add(member1);
                conversation.Members.Add(member2);

                // Step 4: CRITICAL - Save members to database
                await _conversationRepository.UpdateAsync(conversation);
                _logger.LogInformation($"Members added to conversation {conversation.Id}");

                // Step 5: Reload from database to ensure all data is loaded
                var savedConversation = await _conversationRepository.GetConversationWithMembersAsync(conversation.Id);
                if (savedConversation == null)
                {
                    throw new Exception("Failed to retrieve saved conversation");
                }

                _logger.LogInformation($"Conversation {savedConversation.Id} fully created with {savedConversation.Members.Count} members");

                return MapToConversationDto(savedConversation, userId1);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating direct conversation: {ex.Message}");
                throw;
            }
        }

        public async Task<ConversationDto> CreateGroupConversationAsync(string groupName, int createdBy, List<int> memberIds)
        {
            try
            {
                // Validate creator exists
                var creator = await _userRepository.GetByIdAsync(createdBy);
                if (creator == null)
                    throw new Exception($"User with ID {createdBy} not found");

                // Validate all members exist
                foreach (var memberId in memberIds)
                {
                    var member = await _userRepository.GetByIdAsync(memberId);
                    if (member == null)
                        throw new Exception($"User with ID {memberId} not found");
                }

                var conversation = new Conversations
                {
                    ConversationType = ConversationType.Group,
                    GroupName = groupName,
                    CreatedBy = createdBy,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                };

                // Step 1: Add conversation
                await _conversationRepository.AddAsync(conversation);
                _logger.LogInformation($"Group conversation '{groupName}' created with ID {conversation.Id}");

                // Step 2: Add all members
                foreach (var memberId in memberIds)
                {
                    var member = new ConversationMembers
                    {
                        ConversationId = conversation.Id,
                        UserId = memberId,
                        Role = memberId == createdBy ? "Admin" : "Member",
                        JoinedAt = DateTime.UtcNow
                    };

                    conversation.Members.Add(member);
                }

                // Step 3: Save members to database
                await _conversationRepository.UpdateAsync(conversation);
                _logger.LogInformation($"{memberIds.Count} members added to group conversation {conversation.Id}");

                // Step 4: Reload to ensure all data is loaded
                var savedConversation = await _conversationRepository.GetConversationWithMembersAsync(conversation.Id);
                if (savedConversation == null)
                {
                    throw new Exception("Failed to retrieve saved group conversation");
                }

                return MapToConversationDto(savedConversation, createdBy);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating group conversation: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Delete group conversation (only admin can)
        /// </summary>
        public async Task DeleteGroupConversationAsync(int conversationId, int requestingUserId)
        {
            try
            {
                var conversation = await _conversationRepository.GetConversationWithMembersAsync(conversationId);

                if (conversation == null)
                    throw new Exception("Conversation not found");

                if (conversation.ConversationType != ConversationType.Group)
                    throw new Exception("Can only delete group conversations");

                // Check if requesting user is admin
                var adminMember = conversation.Members.FirstOrDefault(m => m.UserId == requestingUserId);
                if (adminMember?.Role != "Admin")
                    throw new Exception("Only admin can delete conversation");

                // Delete conversation and all related data
                await _conversationRepository.DeleteAsync(conversation);
                _logger.LogInformation($"Group conversation {conversationId} deleted by user {requestingUserId}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting conversation: {ex.Message}");
                throw;
            }
        }

        public async Task<ConversationDto> GetConversationAsync(int conversationId)
        {
            try
            {
                var conversation = await _conversationRepository.GetConversationWithMembersAsync(conversationId);
                return conversation != null ? MapToConversationDto(conversation, null) : null!;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting conversation: {ex.Message}");
                throw;
            }
        }

        public async Task<IEnumerable<ConversationDto>> GetUserConversationsAsync(int userId)
        {
            try
            {
                var conversations = await _conversationRepository.GetUserConversationsAsync(userId);
                return conversations.Select(c => MapToConversationDto(c, userId)).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting user conversations: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Leave conversation (any member can)
        /// </summary>
        public async Task LeaveConversationAsync(int conversationId, int userId)
        {
            try
            {
                var conversation = await _conversationRepository.GetConversationWithMembersAsync(conversationId);

                if (conversation == null)
                    throw new Exception("Conversation not found");

                var member = conversation.Members.FirstOrDefault(m => m.UserId == userId);
                if (member != null)
                {
                    conversation.Members.Remove(member);

                    // If leaving user is admin and is last member, delete conversation
                    if (member.Role == "Admin" && !conversation.Members.Any())
                    {
                        await _conversationRepository.DeleteAsync(conversation);
                    }
                    else
                    {
                        await _conversationRepository.UpdateAsync(conversation);
                    }

                    _logger.LogInformation($"User {userId} left conversation {conversationId}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error leaving conversation: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> TogglePinConversationAsync(int conversationId, int userId)
        {
            try
            {
                var member = await _conversationRepository.GetConversationMemberAsync(conversationId, userId);
                if (member == null)
                    throw new Exception("Member not found in conversation");

                member.IsPinned = !member.IsPinned;
                await _memberRepository.UpdateAsync(member);
                
                _logger.LogInformation($"Conversation {conversationId} pin status toggled to {member.IsPinned} for user {userId}");
                return member.IsPinned;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error toggling pin status: {ex.Message}");
                throw;
            }
        }

        public async Task RemoveMemberFromConversationAsync(int conversationId, int userId)
        {
            try
            {
                var conversation = await _conversationRepository.GetConversationWithMembersAsync(conversationId);

                if (conversation != null)
                {
                    var member = conversation.Members.FirstOrDefault(m => m.UserId == userId);
                    if (member != null)
                    {
                        conversation.Members.Remove(member);
                        await _conversationRepository.UpdateAsync(conversation);
                        _logger.LogInformation($"Member {userId} removed from conversation {conversationId}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error removing member: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Transfer admin rights to another member
        /// </summary>
    public async Task TransferAdminRightsAsync(int conversationId, int fromUserId, int toUserId)
        {
            try
            {
                var conversation = await _conversationRepository.GetConversationWithMembersAsync(conversationId);

                if (conversation == null)
                    throw new Exception("Conversation not found");

                // Check if fromUser is admin
                var fromMember = conversation.Members.FirstOrDefault(m => m.UserId == fromUserId);
                if (fromMember?.Role != "Admin")
                    throw new Exception("Only admin can transfer rights");

                // Check if toUser exists in group
                var toMember = conversation.Members.FirstOrDefault(m => m.UserId == toUserId);
                if (toMember == null)
                    throw new Exception("User not in conversation");

                // Transfer rights
                fromMember.Role = "Member";
                toMember.Role = "Admin";

                conversation.CreatedBy = toUserId;
                conversation.UpdatedAt = DateTime.UtcNow;

                await _conversationRepository.UpdateAsync(conversation);
                _logger.LogInformation($"Admin rights transferred from {fromUserId} to {toUserId} in conversation {conversationId}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error transferring admin rights: {ex.Message}");
                throw;
            }
        }

        public async Task<IEnumerable<LinkDto>> GetConversationLinksAsync(int conversationId)
        {
            try
            {
                // 1. Get messages containing "http"
                var messages = await _conversationRepository.GetMessagesWithContentAsync(conversationId, "http");

                var links = new List<LinkDto>();
                // Regex pattern for URL extraction (basic version)
                var urlPattern = @"(http|https)://([\w_-]+(?:(?:\.[\w_-]+)+))([\w.,@?^=%&:/~+#-]*[\w@?^=%&/~+#-])?";

                foreach (var msg in messages)
                {
                    if (string.IsNullOrEmpty(msg.Content)) continue;

                    var matches = Regex.Matches(msg.Content, urlPattern);
                    foreach (Match match in matches)
                    {
                        // Exclude Giphy links
                        if (match.Value.Contains("giphy.com", StringComparison.OrdinalIgnoreCase))
                            continue;

                        links.Add(new LinkDto
                        {
                            MessageId = msg.Id,
                            Url = match.Value,
                            SenderName = msg.Sender?.DisplayName ?? "Unknown",
                            SentAt = msg.CreatedAt
                        });
                    }
                }

                return links.OrderByDescending(l => l.SentAt);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting conversation links: {ex.Message}");
                throw;
            }
        }

        private ConversationDto MapToConversationDto(Conversations conversation, int? currentUserId = null)
        {
            var dto = new ConversationDto
            {
                Id = conversation.Id,
                ConversationType = conversation.ConversationType,
                GroupName = conversation.GroupName,

                Members = conversation.Members
                    .Where(m => m.User != null)
                    .Select(m => new UserDto
                    {
                        Id = m.User.Id,
                        UserName = m.User.UserName!,
                        DisplayName = m.User.DisplayName,
                        Avatar = m.User.Avatar ?? "",
                        Status = m.User.Status,
                    }).ToList(),

                Messages = conversation.Messages?
                    .OrderByDescending(m => m.CreatedAt)
                    .Select(m => new MessageDto
                    {
                        Id = m.Id,
                        ConversationId = m.ConversationId,
                        SenderId = m.SenderId,
                        Content = m.Content,
                        MessageType = m.MessageType,
                        CreatedAt = m.CreatedAt,
                        Sender = m.Sender != null! ? new UserDto
                        {
                            Id = m.Sender.Id,
                            UserName = m.Sender.UserName ?? "",
                            DisplayName = m.Sender.DisplayName ?? "",
                            Avatar = m.Sender.Avatar ?? "",
                            Status = m.Sender.Status,
                        } : null!
                    })
                    .ToList() ?? new List<MessageDto>(),
                CreatedBy = conversation.CreatedBy,
                CreatedAt = conversation.CreatedAt,
            };

            if (currentUserId.HasValue)
            {
                var currentMember = conversation.Members.FirstOrDefault(m => m.UserId == currentUserId.Value);
                if (currentMember != null)
                {
                    dto.IsPinned = currentMember.IsPinned;
                }
            }

            return dto;
        }
    }
}
