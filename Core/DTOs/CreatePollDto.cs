using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Core.DTOs
{
    public class CreatePollDto
    {
        [Required]
        [StringLength(500, MinimumLength = 1)]
        public string Question { get; set; } = string.Empty;

        [Required]
        [MinLength(2, ErrorMessage = "A poll must have at least 2 options.")]
        public List<string> Options { get; set; } = new List<string>();

        public bool AllowMultipleVotes { get; set; }

        public DateTime? EndsAt { get; set; }

        [Required]
        public int ConversationId { get; set; }
    }
}
