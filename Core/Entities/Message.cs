using Core.Enums;

namespace Core.Entities
{
    public class Message
    {
        public int Id { get; set; }
        public int ConversationId { get; set; }
        public int SenderId { get; set; }
        public string? Content { get; set; }
        public MessageType MessageType { get; set; } // Text, Image, File
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool IsDeleted { get; set; } = false;

        // Foreign keys
        public Conversations Conversation { get; set; } = null!;
        public User Sender { get; set; } = null!;

        // Navigation properties
        public ICollection<MessageReaction> Reactions { get; set; } = new List<MessageReaction>();
        public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
        public ICollection<MessageDeletedForUser> DeletedForUsers { get; set; } = new List<MessageDeletedForUser>();
    }
}
