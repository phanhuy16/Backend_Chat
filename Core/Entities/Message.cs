using Core.Enums;
using System.ComponentModel.DataAnnotations.Schema;

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
        public bool IsPinned { get; set; } = false;
        public bool IsModified { get; set; } = false;
        public int? ParentMessageId { get; set; }
        public int? ForwardedFromId { get; set; }
        public DateTime? ScheduledAt { get; set; }

        // Self-destructing message fields
        public int? SelfDestructAfterSeconds { get; set; }  // Time in seconds before message deletes after being viewed
        public DateTime? ViewedAt { get; set; }  // When recipient first viewed the message
        public DateTime? ExpiresAt { get; set; }  // Calculated expiration time (ViewedAt + SelfDestructAfterSeconds)

        // Foreign keys
        public Conversations Conversation { get; set; } = null!;
        public User Sender { get; set; } = null!;
        
        public int? PollId { get; set; }
        [ForeignKey("PollId")]
        public Poll? Poll { get; set; }

        // Navigation properties
        public ICollection<MessageReaction> Reactions { get; set; } = new List<MessageReaction>();
        public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
        public ICollection<MessageDeletedForUser> DeletedForUsers { get; set; } = new List<MessageDeletedForUser>();
        public Message? ParentMessage { get; set; }
        public ICollection<Message> Replies { get; set; } = new List<Message>();
        public ICollection<MessageMention> Mentions { get; set; } = new List<MessageMention>();
    }
}
