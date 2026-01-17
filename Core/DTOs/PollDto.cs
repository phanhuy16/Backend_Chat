using System;
using System.Collections.Generic;

namespace Core.DTOs
{
    public class PollDto
    {
        public int Id { get; set; }
        public string Question { get; set; } = string.Empty;
        public bool AllowMultipleVotes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? EndsAt { get; set; }
        public int CreatorId { get; set; }
        public string CreatorName { get; set; } = string.Empty;
        public int ConversationId { get; set; }
        public List<PollOptionDto> Options { get; set; } = new List<PollOptionDto>();
        public bool HasVoted { get; set; }
        public int TotalVotes { get; set; }
    }

    public class PollOptionDto
    {
        public int Id { get; set; }
        public string Text { get; set; } = string.Empty;
        public int VoteCount { get; set; }
        public bool IsVotedByCurrentUser { get; set; }
        public List<PollVoteDto> Votes { get; set; } = new List<PollVoteDto>();
    }

    public class PollVoteDto
    {
         public int UserId { get; set; }
         public string Username { get; set; } = string.Empty;
         public string? AvatarUrl { get; set; }
    }
}
