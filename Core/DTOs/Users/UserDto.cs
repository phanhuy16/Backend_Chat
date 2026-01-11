using Core.Enums;

namespace Core.DTOs.Users
{
    public class UserDto
    {
        public int Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Avatar { get; set; } = string.Empty;
        public string? Bio { get; set; }
        public string? CustomStatus { get; set; }
        public StatusUser Status { get; set; } = StatusUser.Offline;
        public DateTime LastActiveAt { get; set; }
        public string LastSeenPrivacy { get; set; } = "everyone";
        public string OnlineStatusPrivacy { get; set; } = "everyone";
        public bool ReadReceiptsEnabled { get; set; } = true;
    }
}
