using Core.Enums;

namespace Core.DTOs.Auth
{
    public class UserAuthDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Avatar { get; set; } = string.Empty;
        public StatusUser Status { get; set; }
        public string LastSeenPrivacy { get; set; } = "everyone";
        public string OnlineStatusPrivacy { get; set; } = "everyone";
        public bool ReadReceiptsEnabled { get; set; } = true;
        public string Role { get; set; } = "User";
    }
}
