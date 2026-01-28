

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

        // Granular Permissions
        public bool CanChangeGroupInfo { get; set; } = false;
        public bool CanAddMembers { get; set; } = false;
        public bool CanRemoveMembers { get; set; } = false;
        public bool CanDeleteMessages { get; set; } = false;
        public bool CanPinMessages { get; set; } = false;
        public bool CanChangePermissions { get; set; } = false;
        public bool IsArchived { get; set; } = false;

        // Foreign keys
        public Conversations Conversation { get; set; } = null!;
        public User User { get; set; } = null!;
    }
}
