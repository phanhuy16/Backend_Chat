using Core.Enums;

namespace Core.DTOs.Blocks
{
    public class BlockedUserDto
    {
        public int Id { get; set; }
        public int BlockerId { get; set; }
        public int BlockedUserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public BlockedUserProfileDto? BlockedUserProfile { get; set; }
    }

    public class BlockedUserProfileDto
    {
        public int Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Avatar { get; set; } = string.Empty;
        public StatusUser Status { get; set; }
    }
}
