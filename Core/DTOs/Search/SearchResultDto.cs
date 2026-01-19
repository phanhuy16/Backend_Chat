using Core.DTOs.Messages;
using Core.DTOs.Users;

namespace Core.DTOs.Search
{
    public class SearchResultDto
    {
        public List<MessageSearchResultDto> Messages { get; set; } = new();
        public List<AttachmentSearchResultDto> Files { get; set; } = new();
        public List<UserSearchResultDto> Users { get; set; } = new();
    }

    public class MessageSearchResultDto
    {
        public int Id { get; set; }
        public int ConversationId { get; set; }
        public string? ConversationName { get; set; }
        public string? ConversationAvatar { get; set; }
        public int SenderId { get; set; }
        public string SenderName { get; set; } = string.Empty;
        public string SenderAvatar { get; set; } = string.Empty;
        public string? Content { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class AttachmentSearchResultDto
    {
        public int Id { get; set; }
        public int MessageId { get; set; }
        public int ConversationId { get; set; }
        public string? ConversationName { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FileUrl { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public DateTime UploadedAt { get; set; }
    }

    public class UserSearchResultDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Avatar { get; set; } = string.Empty;
        public string? Bio { get; set; }
        public bool IsContact { get; set; }
    }
}
