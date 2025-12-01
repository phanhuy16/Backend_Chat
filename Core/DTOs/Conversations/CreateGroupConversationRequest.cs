

namespace Core.DTOs.Conversations
{
    public class CreateGroupConversationRequest
    {
        public string GroupName { get; set; } = string.Empty;
        public int CreatedBy { get; set; }
        public List<int> MemberIds { get; set; } = new List<int>();
    }
}
