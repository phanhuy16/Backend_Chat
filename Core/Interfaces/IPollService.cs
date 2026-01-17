using Core.DTOs;
using Core.DTOs.Messages;
using Core.Entities;

namespace Core.Interfaces
{
    public interface IPollService
    {
        Task<MessageDto> CreatePollAsync(int userId, CreatePollDto createPollDto);
        Task<PollDto> VoteAsync(int userId, int pollId, int optionId);
        Task<PollDto> GetPollAsync(int pollId, int currentUserId);
        Task<PollDto> RemoveVoteAsync(int userId, int pollId, int optionId);
    }
}
