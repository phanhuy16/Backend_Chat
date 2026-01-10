using Core.DTOs.ChatHub;
using Core.Enums;
using Core.Interfaces.IRepositories;
using Core.Interfaces.IServices;
using Microsoft.AspNetCore.SignalR;

namespace API.Hubs
{
    public class ChatHub : Hub
    {
        private readonly IMessageService _messageService;
        private readonly IConversationService _conversationService;
        private readonly IChatHubService _chatHubService;
        private readonly IUserService _userService;
        private readonly IBlockedUserRepository _blockedUserRepository;
        private readonly ILogger<ChatHub> _logger;

        public ChatHub(IMessageService messageService, IUserService userService, IConversationService conversationService, IChatHubService chatHubService, IBlockedUserRepository blockedUserRepository, ILogger<ChatHub> logger)
        {
            _messageService = messageService;
            _conversationService = conversationService;
            _chatHubService = chatHubService;
            _userService = userService;
            _blockedUserRepository = blockedUserRepository;
            _logger = logger;
        }

        // Message 
        // User joins a conversation
        public async Task JoinConversation(int conversationId, int userId)
        {
            try
            {
                string groupName = $"conversation_{conversationId}";
                await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
                await _chatHubService.AddUserConnectionAsync(userId, conversationId, Context.ConnectionId);

                // Store connection info
                await Clients.Groups(groupName).SendAsync("UserJoined", new UserStatusUpdate
                {
                    UserId = userId,
                    Status = StatusUser.Online,
                });

                await Clients.Caller.SendAsync("ConnectionEstablished", new { message = "Connected to conversation" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error joining conversation: {ex.Message}");
                await Clients.Caller.SendAsync("Error", $"Error joining conversation: {ex.Message}");
            }
        }

        // User leaves a conversation
        public async Task LeaveConversation(int conversationId, int userId)
        {
            try
            {
                string groupName = $"conversation_{conversationId}";
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);

                // Remove connection info
                await _chatHubService.RemoveUserConnectionAsync(userId, conversationId, Context.ConnectionId);

                // Notify others
                await Clients.Group(groupName).SendAsync("UserLeft", new UserStatusUpdate
                {
                    UserId = userId,
                    Status = StatusUser.Offline,
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error leaving conversation: {ex.Message}");
                await Clients.Caller.SendAsync("Error", $"Error leaving conversation: {ex.Message}");
            }
        }

        // Send message to conversation
        public async Task SendMessage(int conversationId, int senderId, string? content, MessageType messageType = 0)
        {
            try
            {
                var conversation = await _conversationService.GetConversationAsync(conversationId);

                if (conversation == null)
                {
                    await Clients.Caller.SendAsync("Error", "Conversation not found");
                    return;
                }

                if (conversation.ConversationType == ConversationType.Direct)
                {
                    // Get the other member ID
                    var otherMemberId = conversation.Members
                        .FirstOrDefault(m => m.Id != senderId)?.Id;

                    if (otherMemberId == null)
                    {
                        await Clients.Caller.SendAsync("Error", "Invalid conversation members");
                        return;
                    }

                    // Check if sender is blocked (2-way check)
                    var isBlocked = await _blockedUserRepository.IsUserBlockedAsync(senderId, otherMemberId.Value);

                    if (isBlocked)
                    {
                        await Clients.Caller.SendAsync("Error", "Không thể gửi tin nhắn - bạn đã bị chặn hoặc đã chặn người dùng này");
                        _logger.LogWarning($"User {senderId} attempted to send message in blocked conversation with {otherMemberId}");
                        return;
                    }
                }

                // For group conversations, no block check needed (can implement group-specific rules if needed)

                var messageDto = await _messageService.SendMessageAsync(conversationId, senderId, content, messageType);

                var sender = await _userService.GetUserByIdAsync(senderId);

                var chatMessage = new ChatMessage()
                {
                    MessageId = messageDto.Id,
                    ConversationId = conversationId,
                    SenderId = senderId,
                    SenderName = sender?.DisplayName ?? sender?.UserName!,
                    SenderAvatar = sender?.Avatar!,
                    Content = messageDto.Content,
                    MessageType = messageDto.MessageType,
                    CreatedAt = messageDto.CreatedAt,
                };

                string groupName = $"conversation_{conversationId}";

                await Clients.Group(groupName).SendAsync("ReceiveMessage", chatMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending message: {ex.Message}");
                await Clients.Caller.SendAsync("Error", $"Error sending message: {ex.Message}");
            }
        }

        // Typing indicator
        public async Task SendTyping(int conversationId, int userId, string username)
        {
            try
            {
                string groupName = $"conversation_{conversationId}";

                var typingIndicator = new TypingIndicator
                {
                    ConversationId = conversationId,
                    UserId = userId,
                    Username = username
                };

                await Clients.GroupExcept(groupName, Context.ConnectionId).SendAsync("UserTyping", typingIndicator);
                _logger.LogInformation($"User {userId} typing in conversation {conversationId}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in SendTyping: {ex.Message}");
            }
        }

        // Stop typing
        public async Task StopTyping(int conversationId, int userId)
        {
            try
            {
                string groupName = $"conversation_{conversationId}";
                await Clients.GroupExcept(groupName, Context.ConnectionId).SendAsync("UserStoppedTyping", userId);
                _logger.LogInformation($"User {userId} stopped typing in conversation {conversationId}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in StopTyping: {ex.Message}");
            }
        }

        // Edit message
        public async Task EditMessage(int messageId, int conversationId, string newContent, int userId)
        {
            try
            {
                var messageDto = await _messageService.EditMessageAsync(messageId, newContent);

                if (messageDto == null)
                {
                    await Clients.Caller.SendAsync("Error", "Message not found");
                    return;
                }

                string groupName = $"conversation_{conversationId}";

                await Clients.Group(groupName).SendAsync("MessageEdited", new
                {
                    MessageId = messageId,
                    NewContent = newContent,
                    EditedAt = DateTime.UtcNow,
                    EditedBy = userId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error editing message: {ex.Message}");
                await Clients.Caller.SendAsync("Error", $"Error editing message: {ex.Message}");
            }
        }

        // Delete message
        public async Task DeleteMessage(int messageId, int conversationId, int userId)
        {
            try
            {
                await _messageService.DeleteMessageAsync(messageId);

                string groupName = $"conversation_{conversationId}";

                await Clients.Group(groupName).SendAsync("MessageDeleted", new
                {
                    MessageId = messageId,
                    DeletedAt = DateTime.UtcNow,
                    DeletedBy = userId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting message: {ex.Message}");
                await Clients.Caller.SendAsync("Error", $"Error deleting message: {ex.Message}");
            }
        }

        // Add reaction to message
        public async Task AddReaction(int messageId, int conversationId, int userId, string emoji, string username)
        {
            try
            {
                await _messageService.AddReactionAsync(messageId, userId, emoji);

                string groupName = $"conversation_{conversationId}";

                var reaction = new ReactionUpdate
                {
                    MessageId = messageId,
                    UserId = userId,
                    Username = username,
                    Emoji = emoji,
                    Action = "add"
                };

                await Clients.Group(groupName).SendAsync("ReactionAdded", reaction);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error adding reaction: {ex.Message}");
                await Clients.Caller.SendAsync("Error", $"Error adding reaction: {ex.Message}");
            }
        }

        // Remove reaction
        public async Task RemoveReaction(int reactionId, int messageId, int conversationId, int userId)
        {
            try
            {
                await _messageService.RemoveReactionAsync(reactionId);

                string groupName = $"conversation_{conversationId}";

                var reaction = new ReactionUpdate
                {
                    MessageId = messageId,
                    UserId = userId,
                    Action = "remove"
                };

                await Clients.Group(groupName).SendAsync("ReactionRemoved", reaction);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error removing reaction: {ex.Message}");
                await Clients.Caller.SendAsync("Error", $"Error removing reaction: {ex.Message}");
            }
        }

        // Mark message as read
        public async Task MarkAsRead(int conversationId, int messageId, int userId)
        {
            try
            {
                string groupName = $"conversation_{conversationId}";

                await Clients.Group(groupName).SendAsync("MessageRead", new
                {
                    MessageId = messageId,
                    ReadBy = userId,
                    ReadAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error marking message as read: {ex.Message}");
                await Clients.Caller.SendAsync("Error", $"Error marking message as read: {ex.Message}");
            }
        }

        // Get online users in conversation
        public async Task GetOnlineUsers(int conversationId)
        {
            try
            {
                var onlineUsers = await _chatHubService.GetOnlineUsersInConversationAsync(conversationId);
                await Clients.Caller.SendAsync("OnlineUsers", onlineUsers);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting online users: {ex.Message}");
                await Clients.Caller.SendAsync("Error", $"Error getting online users: {ex.Message}");
            }
        }

        public async Task NotifyNewConversation(int userId, int conversationId)
        {
            try
            {
                _logger.LogInformation($"Notifying user {userId} about new conversation {conversationId}");

                // Send to the specific user
                await Clients.User(userId.ToString()).SendAsync("NewConversationCreated", new
                {
                    ConversationId = conversationId,
                    Message = "A new conversation has been created"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Error notifying new conversation: {ex.Message}");
            }
        }

        public async Task NotifyFriendRequest(int receiverId, int senderId, string senderName, string senderAvatar)
        {
            try
            {
                _logger.LogInformation($"Notifying user {receiverId} about friend request from {senderId}");

                await Clients.User(receiverId.ToString()).SendAsync("FriendRequestReceived", new
                {
                    SenderId = senderId,
                    SenderName = senderName,
                    SenderAvatar = senderAvatar,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Error notifying friend request: {ex.Message}");
            }
        }

        // Disconnect handler
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await _chatHubService.RemoveAllUserConnectionsAsync(Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }
    }
}

