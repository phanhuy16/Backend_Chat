using Core.DTOs.Users;

namespace Core.DTOs.Conversations
{
    public class ConversationMemberDto
    {
        public int Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Avatar { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        
        // Permissions
        public bool CanChangeGroupInfo { get; set; }
        public bool CanAddMembers { get; set; }
        public bool CanRemoveMembers { get; set; }
        public bool CanDeleteMessages { get; set; }
        public bool CanPinMessages { get; set; }
        public bool CanChangePermissions { get; set; }
    }
}
