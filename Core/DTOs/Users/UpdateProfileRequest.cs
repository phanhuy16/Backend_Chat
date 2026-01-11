

namespace Core.DTOs.Users
{
    public class UpdateProfileRequest
    {
        public string DisplayName { get; set; } = string.Empty;
        public string? Bio { get; set; }
        public string? Avatar { get; set; }
        public string? LastSeenPrivacy { get; set; }
        public string? OnlineStatusPrivacy { get; set; }
        public bool? ReadReceiptsEnabled { get; set; }
    }
}
