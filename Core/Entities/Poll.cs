using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Entities
{
    public class Poll
    {
        public int Id { get; set; }

        public string Question { get; set; } = string.Empty;

        public bool AllowMultipleVotes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? EndsAt { get; set; }

        public int CreatorId { get; set; }
        [ForeignKey("CreatorId")]
        public User Creator { get; set; } = null!;

        public int ConversationId { get; set; }
        [ForeignKey("ConversationId")]
        public Conversations Conversation { get; set; } = null!;

        public ICollection<PollOption> Options { get; set; } = new List<PollOption>();
    }
}
