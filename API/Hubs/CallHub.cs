using Core.Entities;
using Core.Enums;
using Core.Interfaces.IRepositories;
using Core.Interfaces.IServices;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Security.Claims;

namespace API.Hubs
{
    public class CallHub : Hub
    {
        private readonly IChatHubService _chatHubService;
        private readonly IUserService _userService;
        private readonly IBlockedUserRepository _blockedUserRepository;
        private readonly ILogger<CallHub> _logger;

        // Global dictionary để lưu userId -> ConnectionId mapping
        private static readonly ConcurrentDictionary<int, string> UserConnections =
            new ConcurrentDictionary<int, string>();

        // Lưu trạng thái cuộc gọi
        private static readonly ConcurrentDictionary<string, Call> ActiveCalls =
            new ConcurrentDictionary<string, Call>();

        // Theo dõi thành viên trong cuộc gọi nhóm: callId -> list of userId
        private static readonly ConcurrentDictionary<string, HashSet<int>> GroupCallMembers =
            new ConcurrentDictionary<string, HashSet<int>>();

        public CallHub(
            IChatHubService chatHubService,
            IUserService userService,
            IBlockedUserRepository blockedUserRepository,
            ILogger<CallHub> logger)
        {
            _chatHubService = chatHubService;
            _userService = userService;
            _blockedUserRepository = blockedUserRepository;
            _logger = logger;
        }

        // Đăng ký user khi kết nối
        public async Task RegisterUser(int userId)
        {
            try
            {
                // Lưu userId -> ConnectionId
                UserConnections.AddOrUpdate(userId, Context.ConnectionId, (key, oldVal) => Context.ConnectionId);

                _logger.LogInformation($"User {userId} registered with connection {Context.ConnectionId}");

                // Thông báo cho các user khác biết user này online
                await Clients.AllExcept(Context.ConnectionId).SendAsync("UserOnlineStatusChanged", new
                {
                    userId = userId,
                    status = "Online",
                    timestamp = DateTime.UtcNow
                });

                await Clients.Caller.SendAsync("UserRegistered", new
                {
                    userId = userId,
                    message = "Successfully registered"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error registering user: {ex.Message}");
                await Clients.Caller.SendAsync("Error", $"Error registering user: {ex.Message}");
            }
        }

        // Lấy danh sách user online
        public async Task GetOnlineUsers()
        {
            try
            {
                var onlineUserIds = UserConnections.Keys.ToList();
                _logger.LogInformation($"Online users count: {onlineUserIds.Count}");

                await Clients.Caller.SendAsync("OnlineUsers", new
                {
                    userIds = onlineUserIds,
                    count = onlineUserIds.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting online users: {ex.Message}");
                await Clients.Caller.SendAsync("Error", ex.Message);
            }
        }

        // User1 gọi User2
        public async Task InitiateCall(int conversationId, int receiverId, string callType)
        {
            try
            {
                // Lấy user ID từ JWT token
                var initiatorId = GetUserIdFromContext();

                if (initiatorId == 0)
                {
                    _logger.LogWarning($"Unauthorized call attempt");
                    await Clients.Caller.SendAsync("Error", "Unauthorized");
                    return;
                }

                // Validate call type enum
                if (!Enum.TryParse<CallType>(callType, true, out var parsedCallType))
                {
                    await Clients.Caller.SendAsync("Error", "Invalid call type");
                    return;
                }

                // Kiểm tra block
                var isBlocked = await _blockedUserRepository.IsUserBlockedAsync(initiatorId, receiverId);
                if (isBlocked)
                {
                    _logger.LogWarning($"Call blocked between {initiatorId} and {receiverId}");
                    await Clients.Caller.SendAsync("Error", "Cannot call - user is blocked");
                    return;
                }

                // Kiểm tra receiver có online không
                if (!UserConnections.TryGetValue(receiverId, out var receiverConnectionId))
                {
                    _logger.LogWarning($"Receiver {receiverId} not online");
                    await Clients.Caller.SendAsync("Error", "Receiver is not online");
                    return;
                }

                // Lấy thông tin caller
                var initiator = await _userService.GetUserByIdAsync(initiatorId);

                // Tạo call session
                var callId = $"call_{DateTime.UtcNow.Ticks}";
                var callSession = new Call
                {
                    CallId = callId,
                    InitiatorId = initiatorId,
                    ReceiverId = receiverId,
                    ConversationId = conversationId,
                    CallType = parsedCallType,
                    Status = CallStatus.Pending,
                    StartedAt = DateTime.UtcNow
                };

                ActiveCalls.TryAdd(callId, callSession);

                _logger.LogInformation($"Call initiated: {initiatorId} -> {receiverId} (Type: {parsedCallType})");

                // Gửi IncomingCall notification tới receiver
                await Clients.Client(receiverConnectionId).SendAsync("IncomingCall", new
                {
                    callId = callId,
                    callerId = initiatorId,
                    callerName = initiator?.DisplayName ?? initiator?.UserName,
                    callerAvatar = initiator?.Avatar,
                    conversationId = conversationId,
                    callType = parsedCallType.ToString(),
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                });

                // Confirm tới caller
                await Clients.Caller.SendAsync("CallInitiated", new
                {
                    callId = callId,
                    status = "Ringing"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error initiating call: {ex.Message}");
                await Clients.Caller.SendAsync("Error", $"Error initiating call: {ex.Message}");
            }
        }

        // Initiate Group Call
        public async Task InitiateGroupCall(int conversationId, string callType, List<int> memberIds)
        {
            try
            {
                var initiatorId = GetUserIdFromContext();
                if (initiatorId == 0) return;

                if (!Enum.TryParse<CallType>(callType, true, out var parsedCallType))
                {
                    await Clients.Caller.SendAsync("Error", "Invalid call type");
                    return;
                }

                var initiator = await _userService.GetUserByIdAsync(initiatorId);
                var callId = $"group_call_{conversationId}_{DateTime.UtcNow.Ticks}";

                _logger.LogInformation($"Group call initiated in conversation {conversationId} by {initiatorId}");

                // Register the group call and its initial participants (the initiator)
                GroupCallMembers.TryAdd(callId, new HashSet<int> { initiatorId });

                // Notify all online members except initiator
                foreach (var memberId in memberIds)
                {
                    if (memberId == initiatorId) continue;

                    if (UserConnections.TryGetValue(memberId, out var connectionId))
                    {
                        await Clients.Client(connectionId).SendAsync("IncomingGroupCall", new
                        {
                            callId = callId,
                            callerId = initiatorId,
                            callerName = initiator?.DisplayName ?? initiator?.UserName,
                            callerAvatar = initiator?.Avatar,
                            conversationId = conversationId,
                            callType = parsedCallType.ToString(),
                            timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                        });
                    }
                }

                await Clients.Caller.SendAsync("CallInitiated", new
                {
                    callId = callId,
                    status = "Ringing",
                    isGroup = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error initiating group call: {ex.Message}");
                await Clients.Caller.SendAsync("Error", $"Error initiating group call: {ex.Message}");
            }
        }

        // Join Ongoing Group Call
        public async Task JoinGroupCall(int conversationId, string callId)
        {
            try
            {
                var userId = GetUserIdFromContext();
                if (userId == 0) return;

                _logger.LogInformation($"User {userId} joining group call {callId}");

                if (GroupCallMembers.TryGetValue(callId, out var participants))
                {
                    var user = await _userService.GetUserByIdAsync(userId);
                    var displayName = user?.DisplayName ?? user?.UserName ?? $"User {userId}";

                    // 1. Notify existing participants about the newcomer
                    // Existing participants will each send an offer to this newcomer
                    List<int> currentParticipants;
                    lock (participants)
                    {
                        currentParticipants = participants.ToList();
                        participants.Add(userId);
                    }

                    foreach (var participantId in currentParticipants)
                    {
                        if (participantId == userId) continue;

                        if (UserConnections.TryGetValue(participantId, out var connectionId))
                        {
                            await Clients.Client(connectionId).SendAsync("UserJoinedGroupCall", new
                            {
                                callId = callId,
                                userId = userId,
                                displayName = displayName
                            });
                        }
                    }
                }
                else
                {
                    _logger.LogWarning($"Group call {callId} not found in active tracking");
                    // If not found, create it (might have been lost on server restart)
                    GroupCallMembers.TryAdd(callId, new HashSet<int> { userId });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error joining group call: {ex.Message}");
            }
        }

        // Gửi WebRTC Offer (caller -> receiver)
        public async Task SendCallOffer(int receiverId, object offer)
        {
            try
            {
                var initiatorId = int.Parse(Context.User?.FindFirst("id")?.Value ?? "0");

                if (!UserConnections.TryGetValue(receiverId, out var receiverConnectionId))
                {
                    _logger.LogWarning($"Receiver {receiverId} not found");
                    await Clients.Caller.SendAsync("Error", "Receiver not found");
                    return;
                }

                _logger.LogInformation($"Sending offer from {initiatorId} to {receiverId}");

                // Gửi offer tới receiver
                await Clients.Client(receiverConnectionId).SendAsync("ReceiveCallOffer", new
                {
                    callerId = initiatorId,
                    offer = offer
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending call offer: {ex.Message}");
                await Clients.Caller.SendAsync("Error", ex.Message);
            }
        }

        // Gửi WebRTC Answer (receiver -> caller)
        public async Task SendCallAnswer(int callerId, object answer)
        {
            try
            {
                var receiverId = int.Parse(Context.User?.FindFirst("id")?.Value ?? "0");

                if (!UserConnections.TryGetValue(callerId, out var callerConnectionId))
                {
                    _logger.LogWarning($"Caller {callerId} not found");
                    await Clients.Caller.SendAsync("Error", "Caller not found");
                    return;
                }

                _logger.LogInformation($"Sending answer from {receiverId} to {callerId}");

                // Gửi answer tới caller
                await Clients.Client(callerConnectionId).SendAsync("ReceiveCallAnswer", new
                {
                    receiverId = receiverId,
                    answer = answer
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending call answer: {ex.Message}");
                await Clients.Caller.SendAsync("Error", ex.Message);
            }
        }

        // Gửi ICE Candidate
        public async Task SendIceCandidate(int recipientId, object candidate)
        {
            try
            {
                var senderId = int.Parse(Context.User?.FindFirst("id")?.Value ?? "0");

                if (!UserConnections.TryGetValue(recipientId, out var recipientConnectionId))
                {
                    // Log nhưng không throw error - ICE candidates có thể bị miss
                    _logger.LogWarning($"Recipient {recipientId} not found for ICE candidate");
                    return;
                }

                // Gửi ICE candidate tới recipient
                await Clients.Client(recipientConnectionId).SendAsync("ReceiveIceCandidate", new
                {
                    senderId = senderId,
                    candidate = candidate
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending ICE candidate: {ex.Message}");
                // Không gửi error tới client - ICE candidates không quan trọng
            }
        }

        // Chấp nhận cuộc gọi (1-on-1)
        public async Task AcceptCall(int callerId)
        {
            try
            {
                var receiverId = GetUserIdFromContext();
                if (receiverId == 0) return;

                if (!UserConnections.TryGetValue(callerId, out var callerConnectionId))
                {
                    _logger.LogWarning($"Caller {callerId} not found");
                    return;
                }

                _logger.LogInformation($"Call accepted by user {receiverId} from {callerId}");

                // Thông báo tới caller rằng cuộc gọi đã được chấp nhận
                await Clients.Client(callerConnectionId).SendAsync("CallAccepted", new
                {
                    acceptedBy = receiverId,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error accepting call: {ex.Message}");
            }
        }

        // Từ chối cuộc gọi
        public async Task RejectCall(int callerId)
        {
            try
            {
                var receiverId = int.Parse(Context.User?.FindFirst("id")?.Value ?? "0");

                if (!UserConnections.TryGetValue(callerId, out var callerConnectionId))
                {
                    _logger.LogWarning($"Caller {callerId} not found");
                    return;
                }

                _logger.LogInformation($"Call rejected by user {receiverId} to {callerId}");

                // Thông báo tới caller rằng cuộc gọi bị từ chối
                await Clients.Client(callerConnectionId).SendAsync("CallRejected", new
                {
                    rejectedBy = receiverId,
                    message = "Call was rejected",
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error rejecting call: {ex.Message}");
            }
        }

        // Kết thúc cuộc gọi
        public async Task EndCall(int recipientId, int durationSeconds)
        {
            try
            {
                var userId = int.Parse(Context.User?.FindFirst("id")?.Value ?? "0");

                if (!UserConnections.TryGetValue(recipientId, out var recipientConnectionId))
                {
                    _logger.LogWarning($"Recipient {recipientId} not found");
                    return;
                }

                _logger.LogInformation($"Call ended between {userId} and {recipientId}, duration: {durationSeconds}s");

                // Thông báo tới recipient rằng cuộc gọi đã kết thúc
                await Clients.Client(recipientConnectionId).SendAsync("CallEnded", new
                {
                    endedBy = userId,
                    duration = durationSeconds,
                    timestamp = DateTime.UtcNow
                });

                // Cũng thông báo tới caller (chính mình)
                await Clients.Caller.SendAsync("CallEnded", new
                {
                    endedBy = userId,
                    duration = durationSeconds,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error ending call: {ex.Message}");
            }
        }

        // Update media state (mute/unmute, camera on/off)
        public async Task UpdateMediaState(int recipientId, bool isAudioEnabled, bool isVideoEnabled)
        {
            try
            {
                var userId = int.Parse(Context.User?.FindFirst("id")?.Value ?? "0");

                if (!UserConnections.TryGetValue(recipientId, out var recipientConnectionId))
                {
                    return;
                }

                _logger.LogInformation(
                    $"Media state updated - User {userId}: Audio={isAudioEnabled}, Video={isVideoEnabled}");

                // Thông báo tới recipient
                await Clients.Client(recipientConnectionId).SendAsync("RemoteMediaStateChanged", new
                {
                    userId = userId,
                    isAudioEnabled = isAudioEnabled,
                    isVideoEnabled = isVideoEnabled,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating media state: {ex.Message}");
            }
        }

        // Xử lý user disconnect
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            try
            {
                // Tìm user ID từ connection ID
                var userId = UserConnections.FirstOrDefault(x => x.Value == Context.ConnectionId).Key;

                if (userId != 0)
                {
                    // Xóa user khỏi danh sách online
                    UserConnections.TryRemove(userId, out _);

                    _logger.LogInformation($"User {userId} disconnected");

                    // Thông báo cho các user khác
                    await Clients.AllExcept(Context.ConnectionId).SendAsync("UserOnlineStatusChanged", new
                    {
                        userId = userId,
                        status = "Offline",
                        timestamp = DateTime.UtcNow
                    });

                    // Remove from all group calls
                    foreach (var callEntry in GroupCallMembers)
                    {
                        lock (callEntry.Value)
                        {
                            if (callEntry.Value.Remove(userId))
                            {
                                _logger.LogInformation($"User {userId} removed from group call record {callEntry.Key}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in OnDisconnectedAsync: {ex.Message}");
            }

            await base.OnDisconnectedAsync(exception);
        }

        // Helper để test connection
        public async Task Ping()
        {
            try
            {
                await Clients.Caller.SendAsync("Pong", new { timestamp = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Ping error: {ex.Message}");
            }
        }

        // Lấy thông tin cuộc gọi
        public async Task GetCallInfo(string callId)
        {
            try
            {
                if (ActiveCalls.TryGetValue(callId, out var callSession))
                {
                    await Clients.Caller.SendAsync("CallInfo", callSession);
                }
                else
                {
                    await Clients.Caller.SendAsync("Error", "Call not found");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting call info: {ex.Message}");
            }
        }

        // Helper method to extract user ID from token
        private int GetUserIdFromContext()
        {
            if (Context.User == null)
            {
                _logger.LogWarning("Context.User is null");
                return 0;
            }

            // Try different claim names (common variations)
            var userIdClaim =
                Context.User.FindFirst("id")?.Value ??
                Context.User.FindFirst("sub")?.Value ??
                Context.User.FindFirst("userId")?.Value ??
                Context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                Context.User.FindFirst("oid")?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
            {
                _logger.LogWarning($"No user ID claim found. Available claims:");
                foreach (var claim in Context.User.Claims)
                {
                    _logger.LogWarning($"  - {claim.Type}: {claim.Value}");
                }
                return 0;
            }

            if (int.TryParse(userIdClaim, out var userId))
            {
                _logger.LogInformation($"User ID extracted: {userId}");
                return userId;
            }

            _logger.LogWarning($"Could not parse user ID: {userIdClaim}");
            return 0;
        }
    }
}
