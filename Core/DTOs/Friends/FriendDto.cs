using Core.Enums;

namespace Core.DTOs.Friends
{
    public class FriendDto
    {
        public int Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Avatar { get; set; } = string.Empty;
        public StatusUser Status { get; set; } // 1: Online, 2: Offline, 3: Away
        public DateTime BecomeFriendAt { get; set; }
    }
}
