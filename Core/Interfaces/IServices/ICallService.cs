using Core.DTOs.Call;

namespace Core.Interfaces.IServices
{
    public interface ICallService
    {
        Task<string> InitiateCallAsync(int initiatorId, int receiverId, int conversationId, string callType);
        Task<bool> AnswerCallAsync(string callId, int receiverId);
        Task<bool> RejectCallAsync(string callId, int receiverId);
        Task<bool> EndCallAsync(string callId, int duration);
        Task<List<CallHistoryDto>> GetCallHistoryAsync(int userId);
        Task<List<CallHistoryDto>> GetConversationCallsAsync(int conversationId);
        Task<bool> UpdateMediaStateAsync(string callId, int userId, bool isMuted, bool isVideoOff);
    }
}
