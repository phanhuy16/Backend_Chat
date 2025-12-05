namespace Core.Entities
{
    public class BlockedUser
    {
        public int Id { get; set; }
        public int BlockerId { get; set; }  // người chặn
        public int BlockedUserId { get; set; }  // người bị chặn
        public DateTime CreatedAt { get; set; }

        // Navigation
        public User? Blocker { get; set; }
        public User? BlockedUserProfile { get; set; }
    }
}
