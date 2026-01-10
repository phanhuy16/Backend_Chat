using Core.Entities;

namespace Core.Interfaces.IRepositories
{
    public interface IAttachmentRepository : IRepository<Attachment>
    {
        Task<IEnumerable<Attachment>> GetMessageAttachmentsAsync(int messageId);
        Task<Attachment> GetAttachmentByIdAsync(int attachmentId);
        Task<IEnumerable<Attachment>> GetConversationAttachmentsAsync(int conversationId);
    }
}
