using Core.DTOs.Call;
using Core.DTOs.Users;
using Core.Entities;
using Core.Enums;
using Core.Interfaces.IRepositories;
using Core.Interfaces.IServices;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services
{
    public class CallService : ICallService
    {
        private readonly ICallRepository _callRepository;
        private readonly IUserRepository _userRepository;
        private readonly IConversationRepository _conversationRepository;
        private readonly ILogger<CallService> _logger;

        public CallService(
            ICallRepository callRepository,
            IUserRepository userRepository,
            IConversationRepository conversationRepository,
            ILogger<CallService> logger)
        {
            _callRepository = callRepository;
            _userRepository = userRepository;
            _conversationRepository = conversationRepository;
            _logger = logger;
        }

        // Initiate call
        public async Task<string> InitiateCallAsync(int initiatorId, int receiverId, int conversationId, string callType)
        {
            try
            {
                // Validate users
                var initiator = await _userRepository.GetByIdAsync(initiatorId);
                var receiver = await _userRepository.GetByIdAsync(receiverId);

                if (initiator == null || receiver == null)
                {
                    throw new Exception("Invalid initiator or receiver");
                }

                // Parse call type
                if (!Enum.TryParse<CallType>(callType, true, out var parsedCallType))
                {
                    throw new Exception("Invalid call type");
                }

                // Validate conversation
                var conversation = await _conversationRepository.GetConversationWithMembersAsync(conversationId);
                if (conversation == null)
                {
                    throw new Exception("Conversation not found");
                }

                // Check if users are in conversation
                var isMemberOfConversation = conversation.Members.Any(m => m.UserId == initiatorId) &&
                                           conversation.Members.Any(m => m.UserId == receiverId);
                if (!isMemberOfConversation)
                {
                    throw new Exception("Users are not members of this conversation");
                }

                // Create call record
                var callId = $"call_{DateTime.UtcNow.Ticks}";
                var call = new Call
                {
                    CallId = callId,
                    InitiatorId = initiatorId,
                    ReceiverId = receiverId,
                    ConversationId = conversationId,
                    CallType = parsedCallType,
                    Status = CallStatus.Pending,
                    StartedAt = DateTime.UtcNow
                };

                await _callRepository.AddAsync(call);

                _logger.LogInformation($"Call initiated: {callId} from {initiatorId} to {receiverId} ({parsedCallType})");

                return callId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating call");
                throw;
            }
        }

        // Answer call
        public async Task<bool> AnswerCallAsync(string callId, int receiverId)
        {
            try
            {
                var call = await _callRepository.GetCallByCallIdAsync(callId);
                if (call == null)
                {
                    throw new Exception($"Call {callId} not found");
                }

                if (call.ReceiverId != receiverId)
                {
                    throw new Exception("Invalid receiver");
                }

                call.Status = CallStatus.Answered; // Use enum
                call.StartedAt = DateTime.UtcNow;

                await _callRepository.UpdateAsync(call);

                _logger.LogInformation($"Call answered: {callId}");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error answering call {callId}");
                throw;
            }
        }

        // Reject call
        public async Task<bool> RejectCallAsync(string callId, int receiverId)
        {
            try
            {
                var call = await _callRepository.GetCallByCallIdAsync(callId);
                if (call == null)
                {
                    throw new Exception($"Call {callId} not found");
                }

                call.Status = CallStatus.Rejected; // Use enum
                call.EndedAt = DateTime.UtcNow;

                await _callRepository.UpdateAsync(call);

                _logger.LogInformation($"Call rejected: {callId}");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error rejecting call {callId}");
                throw;
            }
        }

        // End call
        public async Task<bool> EndCallAsync(string callId, int duration)
        {
            try
            {
                var call = await _callRepository.GetCallByCallIdAsync(callId);
                if (call == null)
                {
                    throw new Exception($"Call {callId} not found");
                }

                // Use enum for status determination
                call.Status = call.Status == CallStatus.Answered ? CallStatus.Completed : CallStatus.Missed;
                call.EndedAt = DateTime.UtcNow;
                call.DurationInSeconds = duration;

                await _callRepository.UpdateAsync(call);

                _logger.LogInformation($"Call ended: {callId}, Status: {call.Status}, Duration: {duration}s");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error ending call {callId}");
                throw;
            }
        }

        // Update media state
        public async Task<bool> UpdateMediaStateAsync(string callId, int userId, bool isMuted, bool isVideoOff)
        {
            try
            {
                var call = await _callRepository.GetCallByCallIdAsync(callId);
                if (call == null)
                {
                    throw new Exception($"Call {callId} not found");
                }

                if (call.InitiatorId == userId)
                {
                    call.IsInitiatorMuted = isMuted;
                    call.IsInitiatorVideoOff = isVideoOff;
                }
                else if (call.ReceiverId == userId)
                {
                    call.IsReceiverMuted = isMuted;
                    call.IsReceiverVideoOff = isVideoOff;
                }
                else
                {
                    throw new Exception("Invalid user for this call");
                }

                await _callRepository.UpdateAsync(call);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating media state for call {callId}");
                throw;
            }
        }

        // Get call history with mapped enums
        public async Task<List<CallHistoryDto>> GetCallHistoryAsync(int userId)
        {
            try
            {
                var calls = await _callRepository.GetCallHistoryAsync(userId);

                var callHistory = calls.Select(c => new CallHistoryDto
                {
                    Id = c.Id,
                    CallId = c.CallId,
                    CallType = c.CallType,
                    Status = c.Status,
                    DurationInSeconds = c.DurationInSeconds,
                    StartedAt = c.StartedAt,
                    EndedAt = c.EndedAt,
                    Initiator = MapUserToDto(c.Initiator),
                    Receiver = MapUserToDto(c.Receiver)
                }).ToList();

                return callHistory;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching call history for user {userId}");
                throw;
            }
        }

        // Get conversation calls
        public async Task<List<CallHistoryDto>> GetConversationCallsAsync(int conversationId)
        {
            try
            {
                var calls = await _callRepository.GetConversationCallsAsync(conversationId);

                var callList = calls.Select(c => new CallHistoryDto
                {
                    Id = c.Id,
                    CallId = c.CallId,
                    CallType = c.CallType,
                    Status = c.Status,
                    DurationInSeconds = c.DurationInSeconds,
                    StartedAt = c.StartedAt,
                    EndedAt = c.EndedAt,
                    Initiator = MapUserToDto(c.Initiator),
                    Receiver = MapUserToDto(c.Receiver)
                }).ToList();

                return callList;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching calls for conversation {conversationId}");
                throw;
            }
        }

        // Helper method
        private UserDto? MapUserToDto(User? user)
        {
            if (user == null) return null;

            return new UserDto
            {
                Id = user.Id,
                UserName = user.UserName ?? string.Empty,
                DisplayName = user.DisplayName,
                Avatar = user.Avatar
            };
        }
    }
}
