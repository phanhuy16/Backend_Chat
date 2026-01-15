using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Entities
{
    public class PushSubscription
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public virtual User User { get; set; } = null!;

        [Required]
        public string Endpoint { get; set; } = string.Empty;

        public string? P256DH { get; set; }
        public string? Auth { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
