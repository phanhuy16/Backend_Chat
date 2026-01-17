using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Entities
{
    public class PollOption
    {
        public int Id { get; set; }

        public string Text { get; set; } = string.Empty;

        public int PollId { get; set; }
        [ForeignKey("PollId")]
        public Poll Poll { get; set; } = null!;

        public ICollection<PollVote> Votes { get; set; } = new List<PollVote>();
    }
}
