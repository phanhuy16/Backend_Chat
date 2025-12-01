

namespace Core.DTOs.Messages
{
    public class AddReactionRequest
    {
        public int UserId { get; set; }
        public string Emoji { get; set; } = string.Empty;
    }
}
