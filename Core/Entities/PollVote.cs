using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Entities
{
    public class PollVote
    {
        public int Id { get; set; }

        public int PollOptionId { get; set; }
        [ForeignKey("PollOptionId")]
        public PollOption PollOption { get; set; } = null!;

        public int UserId { get; set; }
        [ForeignKey("UserId")]
        public User User { get; set; } = null!;

        public DateTime VotedAt { get; set; } = DateTime.UtcNow;
    }
}
