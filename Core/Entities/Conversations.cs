using Core.Enums;
namespace Core.Entities
{
    public class Conversations
    {
        public int Id { get; set; }
        public ConversationType ConversationType { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public int CreatedBy { get; set; }  
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Foreign key
        public User Creator { get; set; } = null!;

        // Navigation properties
        public ICollection<ConversationMembers> Members { get; set; } = new List<ConversationMembers>();
        public ICollection<Message> Messages { get; set; } = new List<Message>();
    }
}
