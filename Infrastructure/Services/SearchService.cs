using Core.DTOs.Search;
using Core.Interfaces.IRepositories;
using Core.Interfaces.IServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services
{
    public class SearchService : ISearchService
    {
        private readonly IMessageRepository _messageRepository;
        private readonly IUserRepository _userRepository;
        private readonly IConversationRepository _conversationRepository;
        private readonly ILogger<SearchService> _logger;

        public SearchService(
            IMessageRepository messageRepository,
            IUserRepository userRepository,
            IConversationRepository conversationRepository,
            ILogger<SearchService> logger)
        {
            _messageRepository = messageRepository;
            _userRepository = userRepository;
            _conversationRepository = conversationRepository;
            _logger = logger;
        }

        public async Task<SearchResultDto> SearchAsync(int userId, string query, int pageSize = 20)
        {
            try
            {
                var result = new SearchResultDto
                {
                    Messages = await SearchMessagesAsync(userId, query, pageSize),
                    Files = await SearchFilesAsync(userId, query, pageSize),
                    Users = await SearchUsersAsync(userId, query, pageSize)
                };

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing global search for user {UserId} with query {Query}", userId, query);
                throw;
            }
        }

        public async Task<List<MessageSearchResultDto>> SearchMessagesAsync(int userId, string query, int pageSize = 20)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query))
                    return new List<MessageSearchResultDto>();

                // Get all conversations the user is a member of
                var userConversations = await _conversationRepository.GetUserConversationsAsync(userId);
                var conversationIds = userConversations.Select(c => c.Id).ToList();

                // Search messages in those conversations
                var messages = await _messageRepository.SearchMessagesGloballyAsync(conversationIds, query, pageSize);

                var results = messages.Select(m => new MessageSearchResultDto
                {
                    Id = m.Id,
                    ConversationId = m.ConversationId,
                    ConversationName = m.Conversation?.GroupName ?? GetDirectChatName(m.Conversation, userId),
                    ConversationAvatar = m.Conversation?.GroupName ?? GetDirectChatAvatar(m.Conversation, userId),
                    SenderId = m.SenderId,
                    SenderName = m.Sender?.DisplayName ?? m.Sender?.UserName ?? "Unknown",
                    SenderAvatar = m.Sender?.Avatar ?? "",
                    Content = m.Content,
                    CreatedAt = m.CreatedAt
                }).ToList();

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching messages for user {UserId} with query {Query}", userId, query);
                throw;
            }
        }

        public async Task<List<AttachmentSearchResultDto>> SearchFilesAsync(int userId, string query, int pageSize = 20)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query))
                    return new List<AttachmentSearchResultDto>();

                // Get all conversations the user is a member of
                var userConversations = await _conversationRepository.GetUserConversationsAsync(userId);
                var conversationIds = userConversations.Select(c => c.Id).ToList();

                // Search attachments in those conversations
                var attachments = await _messageRepository.SearchAttachmentsAsync(conversationIds, query, pageSize);

                var results = attachments.Select(a => new AttachmentSearchResultDto
                {
                    Id = a.Id,
                    MessageId = a.MessageId,
                    ConversationId = a.Message?.ConversationId ?? 0,
                    ConversationName = a.Message?.Conversation?.GroupName ?? GetDirectChatName(a.Message?.Conversation, userId),
                    FileName = a.FileName,
                    FileUrl = a.FileUrl,
                    FileType = a.FileType,
                    FileSize = a.FileSize,
                    UploadedAt = a.UploadedAt
                }).ToList();

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching files for user {UserId} with query {Query}", userId, query);
                throw;
            }
        }

        public async Task<List<UserSearchResultDto>> SearchUsersAsync(int userId, string query, int pageSize = 20)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query))
                    return new List<UserSearchResultDto>();

                // Search all users by display name or username
                var users = await _userRepository.SearchUsersAsync(query, pageSize);

                // Get user's contacts to mark them
                var userContacts = await _userRepository.GetContactsAsync(userId);
                var contactIds = userContacts.Select(c => c.Id).ToHashSet();

                var results = users.Select(u => new UserSearchResultDto
                {
                    Id = u.Id,
                    Username = u.UserName ?? "",
                    DisplayName = u.DisplayName,
                    Avatar = u.Avatar,
                    Bio = u.Bio,
                    IsContact = contactIds.Contains(u.Id)
                }).ToList();

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching users for user {UserId} with query {Query}", userId, query);
                throw;
            }
        }

        private string GetDirectChatName(Core.Entities.Conversations? conversation, int userId)
        {
            if (conversation == null || conversation.ConversationType == Core.Enums.ConversationType.Group)
                return "";

            var otherMember = conversation.Members?.FirstOrDefault(m => m.UserId != userId);
            return otherMember?.User?.DisplayName ?? otherMember?.User?.UserName ?? "Unknown";
        }

        private string GetDirectChatAvatar(Core.Entities.Conversations? conversation, int userId)
        {
            if (conversation == null || conversation.ConversationType == Core.Enums.ConversationType.Group)
                return "";

            var otherMember = conversation.Members?.FirstOrDefault(m => m.UserId != userId);
            return otherMember?.User?.Avatar ?? "";
        }
    }
}
