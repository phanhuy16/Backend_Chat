using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Entities
{
    public class MessageMention
    {
        public int Id { get; set; }
        public int MessageId { get; set; }
        public int UserId { get; set; }
        public DateTime MentionedAt { get; set; } = DateTime.UtcNow;

        // Foreign keys
        [ForeignKey("MessageId")]
        public Message Message { get; set; } = null!;

        [ForeignKey("UserId")]
        public User User { get; set; } = null!;
    }
}
