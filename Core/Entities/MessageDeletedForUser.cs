using System.ComponentModel.DataAnnotations;

namespace Core.Entities
{
    public class MessageDeletedForUser
    {
        [Key]
        public int Id { get; set; }
        
        public int MessageId { get; set; }
        
        public int UserId { get; set; }
        
        public DateTime DeletedAt { get; set; }
        
        // Navigation properties
        public Message Message { get; set; } = null!;
        public User User { get; set; } = null!;
    }
}
