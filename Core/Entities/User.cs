using Core.Enums;
using Microsoft.AspNetCore.Identity;

namespace Core.Entities
{
    public class User : IdentityUser<int>
    {
        public string DisplayName { get; set; } = string.Empty;
        public string Avatar { get; set; } = string.Empty;
        public StatusUser Status { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime RefreshTokenExpiryTime { get; set; }

        // Navigation properties
        public ICollection<Conversations> CreatedConversations { get; set; } = new List<Conversations>();
        public ICollection<ConversationMembers> ConversationMembers { get; set; } = new List<ConversationMembers>();
        public ICollection<Message> SentMessages { get; set; } = new List<Message>();
        public ICollection<MessageReaction> MessageReactions { get; set; } = new List<MessageReaction>();
        public ICollection<UserContact> UserContacts { get; set; } = new List<UserContact>();
        public ICollection<UserContact> ContactedByUsers { get; set; } = new List<UserContact>();
    }
}
