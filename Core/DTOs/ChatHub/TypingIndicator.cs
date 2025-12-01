using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTOs.ChatHub
{
    public class TypingIndicator
    {
        public int ConversationId { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
    }
}
