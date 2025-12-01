using Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTOs.ChatHub
{
    public class UserStatusUpdate
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public StatusUser Status { get; set; }
    }
}
