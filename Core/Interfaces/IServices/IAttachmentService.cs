using Core.DTOs.Attachments;
using Microsoft.AspNetCore.Http;

namespace Core.Interfaces.IServices
{
    public interface IAttachmentService
    {
        Task<AttachmentDto> SaveAttachmentAsync(int messageId, IFormFile file);
        Task<bool> DeleteAttachmentAsync(int attachmentId);
        Task<IEnumerable<AttachmentDto>> GetAttachmentsByConversationIdAsync(int conversationId);
    }
}
