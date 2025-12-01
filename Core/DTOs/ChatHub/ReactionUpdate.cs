using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTOs.ChatHub
{
    public class ReactionUpdate
    {
        public int MessageId { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Emoji { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
    }
}
