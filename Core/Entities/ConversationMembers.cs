

namespace Core.Entities
{
    public class ConversationMembers
    {
        public int Id { get; set; }
        public int ConversationId { get; set; }
        public int UserId { get; set; }
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
        public string Role { get; set; } = string.Empty;
        public bool IsPinned { get; set; } = false;

        // Foreign keys
        public Conversations Conversation { get; set; } = null!;
        public User User { get; set; } = null!;
    }
}
