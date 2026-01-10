

using Core.DTOs.Friends;
using Core.Entities;
using Core.Enums;
using Core.Interfaces.IRepositories;
using Core.Interfaces.IServices;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services
{
    public class FriendService : IFriendService
    {
        private readonly IUserContactRepository _userContactRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<FriendService> _logger;

        public FriendService(
            IUserContactRepository userContactRepository,
            IUserRepository userRepository,
            ILogger<FriendService> logger)
        {
            _userContactRepository = userContactRepository;
            _userRepository = userRepository;
            _logger = logger;
        }

        // Accept friend request
        public async Task<bool> AcceptFriendRequestAsync(int requestId, int userId)
        {
            try
            {
                var contact = await _userContactRepository.GetByIdAsync(requestId);
                if (contact == null)
                {
                    _logger.LogWarning($"Friend request {requestId} not found");
                    return false;
                }

                // Verify user is the receiver
                if (contact.ReceiverId != userId)
                {
                    _logger.LogWarning($"User {userId} is not receiver of request {requestId}");
                    return false;
                }

                // Update status to accepted
                contact.Status = StatusFriend.Accepted; // Accepted
                contact.BecomeFriendAt = DateTime.UtcNow;
                contact.UpdatedAt = DateTime.UtcNow;

                await _userContactRepository.UpdateAsync(contact);
                _logger.LogInformation($"Friend request {requestId} accepted by user {userId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error accepting friend request {requestId}");
                throw;
            }
        }

        // Check if users are friends
        public async Task<bool> AreFriendsAsync(int userId1, int userId2)
        {
            try
            {
                return await _userContactRepository.AreFriendsAsync(userId1, userId2);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking friendship between {userId1} and {userId2}");
                throw;
            }
        }

        // Get friends list
        public async Task<IEnumerable<FriendDto>> GetFriendsAsync(int userId)
        {
            try
            {
                var friends = await _userContactRepository.GetFriendsAsync(userId);
                var friendDtos = new List<FriendDto>();

                foreach (var contact in friends)
                {
                    // Get the friend (not self)
                    var friend = contact.SenderId == userId ? contact.Receiver : contact.Sender;
                    friendDtos.Add(MapToFriendDto(friend, contact.BecomeFriendAt ?? DateTime.UtcNow));
                }

                return friendDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting friends for user {userId}");
                throw;
            }
        }

        // Get pending friend requests (received)
        public async Task<IEnumerable<FriendRequestDto>> GetPendingRequestsAsync(int userId)
        {
            try
            {
                var requests = await _userContactRepository.GetPendingRequestsAsync(userId);
                return requests.Select(r => MapToFriendRequestDto(r)).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting pending requests for user {userId}");
                throw;
            }
        }

        // Get sent friend requests
        public async Task<IEnumerable<FriendRequestDto>> GetSentRequestsAsync(int userId)
        {
            try
            {
                var requests = await _userContactRepository.GetSentRequestsAsync(userId);
                return requests.Select(r => MapToFriendRequestDto(r)).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting sent requests for user {userId}");
                throw;
            }
        }

        // Reject friend request
        public async Task<bool> RejectFriendRequestAsync(int requestId, int userId)
        {
            try
            {
                var contact = await _userContactRepository.GetByIdAsync(requestId);
                if (contact == null)
                {
                    _logger.LogWarning($"Friend request {requestId} not found");
                    return false;
                }

                // Verify user is the receiver
                if (contact.ReceiverId != userId)
                {
                    _logger.LogWarning($"User {userId} is not receiver of request {requestId}");
                    return false;
                }

                // Update status to rejected
                contact.Status = StatusFriend.Rejected; // Rejected
                contact.UpdatedAt = DateTime.UtcNow;

                await _userContactRepository.UpdateAsync(contact);
                _logger.LogInformation($"Friend request {requestId} rejected by user {userId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error rejecting friend request {requestId}");
                throw;
            }
        }

        // Remove friend
        public async Task<bool> RemoveFriendAsync(int userId, int friendId)
        {
            try
            {
                var contact = await _userContactRepository.GetFriendRequestAsync(userId, friendId);
                if (contact == null)
                {
                    _logger.LogWarning($"Friendship between {userId} and {friendId} not found");
                    return false;
                }

                await _userContactRepository.DeleteAsync(contact.Id);
                _logger.LogInformation($"User {userId} removed friend {friendId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error removing friend {friendId} for user {userId}");
                throw;
            }
        }

        // Send friend request
        public async Task<bool> SendFriendRequestAsync(int senderId, int receiverId)
        {
            try
            {
                // Validate users exist
                var sender = await _userRepository.GetByIdAsync(senderId);
                var receiver = await _userRepository.GetByIdAsync(receiverId);

                if (sender == null || receiver == null)
                {
                    _logger.LogWarning($"User not found: sender={senderId}, receiver={receiverId}");
                    return false;
                }

                // Check if already friends or request exists
                var existing = await _userContactRepository.GetFriendRequestAsync(senderId, receiverId);
                if (existing != null)
                {
                    _logger.LogWarning($"Contact already exists between {senderId} and {receiverId}");
                    return false;
                }

                // Create new friend request
                var contact = new UserContact
                {
                    SenderId = senderId,
                    ReceiverId = receiverId,
                    Status = StatusFriend.Pending, // Pending
                    CreatedAt = DateTime.UtcNow
                };

                await _userContactRepository.AddAsync(contact);
                _logger.LogInformation($"Friend request sent from {senderId} to {receiverId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending friend request from {senderId} to {receiverId}");
                throw;
            }
        }

        // Helper: Map to FriendRequestDto
        private FriendRequestDto MapToFriendRequestDto(UserContact contact)
        {
            return new FriendRequestDto
            {
                Id = contact.Id,
                SenderId = contact.SenderId,
                SenderName = contact.Sender?.DisplayName ?? contact.Sender?.UserName ?? "Unknown",
                SenderAvatar = contact.Sender?.Avatar ?? "",
                ReceiverId = contact.ReceiverId,
                ReceiverName = contact.Receiver?.DisplayName ?? contact.Receiver?.UserName ?? "Unknown",
                ReceiverAvatar = contact.Receiver?.Avatar ?? "",
                Status = contact.Status,
                CreatedAt = contact.CreatedAt
            };
        }

        // Helper: Map to FriendDto
        private FriendDto MapToFriendDto(User user, DateTime becomeFriendAt)
        {
            return new FriendDto
            {
                Id = user.Id,
                UserName = user.UserName ?? "",
                DisplayName = user.DisplayName,
                Avatar = user.Avatar,
                Status = user.Status,
                BecomeFriendAt = becomeFriendAt
            };
        }
    }
}
