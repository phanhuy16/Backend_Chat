using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTOs.Messages
{
    public class EditMessageRequest
    {
        public string Content { get; set; } = string.Empty;
        public List<int> MentionedUserIds { get; set; } = new List<int>();
    }
}
