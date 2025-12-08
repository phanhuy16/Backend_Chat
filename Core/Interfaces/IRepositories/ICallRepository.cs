using Core.DTOs.Call;
using Core.Entities;

namespace Core.Interfaces.IRepositories
{
    public interface ICallRepository : IRepository<Call>
    {
        Task<List<Call>> GetCallHistoryAsync(int userId, int pageNumber = 1, int pageSize = 10);
        Task<List<Call>> GetConversationCallsAsync(int conversationId, int pageNumber = 1, int pageSize = 10);
        Task<Call?> GetCallByIdAsync(int callId);
        Task<Call?> GetCallByCallIdAsync(string callId);
        Task<int> GetTotalCallDurationAsync(int userId);
        Task<int> GetMissedCallsCountAsync(int userId);

        /// <summary>
        /// Lấy thống kê cuộc gọi
        /// </summary>
        Task<CallStatistics> GetCallStatisticsAsync(int userId);
    }
}
