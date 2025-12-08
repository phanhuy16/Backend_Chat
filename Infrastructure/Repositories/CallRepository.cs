using Core.DTOs.Call;
using Core.Entities;
using Core.Enums;
using Core.Interfaces.IRepositories;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Repositories
{
    public class CallRepository : Repository<Call>, ICallRepository
    {
        private readonly ILogger<CallRepository> _logger;

        public CallRepository(ChatAppDbContext context, ILogger<CallRepository> logger)
            : base(context)
        {
            _logger = logger;
        }

        // Get call history for user
        public async Task<List<Call>> GetCallHistoryAsync(int userId, int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                var skip = (pageNumber - 1) * pageSize;

                return await _context.Calls
                    .Include(c => c.Initiator)
                    .Include(c => c.Receiver)
                    .Include(c => c.Conversation)
                    .Where(c => c.InitiatorId == userId || c.ReceiverId == userId)
                    .OrderByDescending(c => c.StartedAt)
                    .Skip(skip)
                    .Take(pageSize)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting call history for user {userId}: {ex.Message}");
                throw;
            }
        }

        // Get calls in conversation
        public async Task<List<Call>> GetConversationCallsAsync(int conversationId, int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                var skip = (pageNumber - 1) * pageSize;

                return await _context.Calls
                    .Include(c => c.Initiator)
                    .Include(c => c.Receiver)
                    .Include(c => c.Conversation)
                    .Where(c => c.ConversationId == conversationId)
                    .OrderByDescending(c => c.StartedAt)
                    .Skip(skip)
                    .Take(pageSize)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting calls for conversation {conversationId}: {ex.Message}");
                throw;
            }
        }

        // Get call by Id
        public async Task<Call?> GetCallByIdAsync(int callId)
        {
            try
            {
                var call = await _context.Calls
                    .Include(c => c.Initiator)
                    .Include(c => c.Receiver)
                    .Include(c => c.Conversation)
                    .FirstOrDefaultAsync(c => c.Id == callId);

                return call;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching call {callId}");
                throw;
            }
        }

        // Get call by CallId (string)
        public async Task<Call?> GetCallByCallIdAsync(string callId)
        {
            try
            {
                var call = await _context.Calls
                    .Include(c => c.Initiator)
                    .Include(c => c.Receiver)
                    .FirstOrDefaultAsync(c => c.CallId == callId);

                return call;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching call {callId}");
                throw;
            }
        }

        // Get total call duration
        public async Task<int> GetTotalCallDurationAsync(int userId)
        {
            try
            {
                var totalDuration = await _context.Calls
                    .Where(c => (c.InitiatorId == userId || c.ReceiverId == userId) && c.Status == CallStatus.Completed)
                    .SumAsync(c => c.DurationInSeconds);

                return totalDuration;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error calculating total duration for user {userId}");
                throw;
            }
        }

        public async Task<List<Call>> GetRecentCallsAsync(int userId, int count = 5)
        {
            try
            {
                return await _context.Calls
                    .Include(c => c.Initiator)
                    .Include(c => c.Receiver)
                    .Include(c => c.Conversation)
                    .Where(c => c.InitiatorId == userId || c.ReceiverId == userId)
                    .Where(c => c.Status != CallStatus.Pending) // Bỏ qua cuộc gọi chưa trả lời
                    .OrderByDescending(c => c.StartedAt)
                    .Take(count)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting recent calls for user {userId}: {ex.Message}");
                throw;
            }
        }

        public async Task<int> GetMissedCallsCountAsync(int userId)
        {
            try
            {
                return await _context.Calls
                    .Where(c => c.ReceiverId == userId && c.Status == CallStatus.Missed)
                    .CountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting missed calls count for user {userId}: {ex.Message}");
                throw;
            }
        }

        public async Task<CallStatistics> GetCallStatisticsAsync(int userId)
        {
            try
            {
                var calls = await _context.Calls
                    .Where(c => c.InitiatorId == userId || c.ReceiverId == userId)
                    .ToListAsync();

                var completedCalls = calls.Where(c => c.Status == CallStatus.Completed).ToList();
                var missedCalls = calls.Where(c => c.Status == CallStatus.Missed).ToList();
                var rejectedCalls = calls.Where(c => c.Status == CallStatus.Rejected).ToList();
                var videoCalls = calls.Where(c => c.CallType == CallType.Video).ToList();
                var audioCalls = calls.Where(c => c.CallType == CallType.Audio).ToList();

                var totalDurationSeconds = completedCalls.Sum(c => c.DurationInSeconds);
                var averageDuration = completedCalls.Count > 0
                    ? (double)totalDurationSeconds / completedCalls.Count
                    : 0;

                return new CallStatistics
                {
                    TotalCalls = calls.Count,
                    CompletedCalls = completedCalls.Count,
                    MissedCalls = missedCalls.Count,
                    RejectedCalls = rejectedCalls.Count,
                    TotalDurationSeconds = totalDurationSeconds,
                    AverageDurationSeconds = averageDuration,
                    VideoCallsCount = videoCalls.Count,
                    AudioCallsCount = audioCalls.Count
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting call statistics for user {userId}: {ex.Message}");
                throw;
            }
        }
    }
}
