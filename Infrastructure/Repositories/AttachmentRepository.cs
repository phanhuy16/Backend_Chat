using Core.Entities;
using Core.Interfaces.IRepositories;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Repositories
{
    public class AttachmentRepository : Repository<Attachment>, IAttachmentRepository
    {
        private readonly ILogger<AttachmentRepository> _logger;

        public AttachmentRepository(
            ChatAppDbContext context,
            ILogger<AttachmentRepository> logger) : base(context)
        {
            _logger = logger;
        }

        /// <summary>
        /// Get all attachments for a specific message
        /// </summary>
        public async Task<IEnumerable<Attachment>> GetMessageAttachmentsAsync(int messageId)
        {
            try
            {
                _logger.LogInformation("Fetching attachments for message {MessageId}", messageId);

                var attachments = await _context.Attachments
                    .Where(a => a.MessageId == messageId)
                    .OrderByDescending(a => a.UploadedAt)
                    .ToListAsync();

                if (!attachments.Any())
                {
                    _logger.LogWarning("No attachments found for message {MessageId}", messageId);
                }

                return attachments;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching attachments for message {MessageId}", messageId);
                throw;
            }
        }

        /// <summary>
        /// Get attachment by ID
        /// </summary>
        public async Task<Attachment> GetAttachmentByIdAsync(int attachmentId)
        {
            try
            {
                _logger.LogInformation("Fetching attachment {AttachmentId}", attachmentId);

                var attachment = await _context.Attachments
                    .FirstOrDefaultAsync(a => a.Id == attachmentId);

                if (attachment == null)
                {
                    _logger.LogWarning("Attachment {AttachmentId} not found", attachmentId);
                }

                return attachment!;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching attachment {AttachmentId}", attachmentId);
                throw;
            }
        }
        /// <summary>
        /// Get all attachments for a specific conversation
        /// </summary>
        public async Task<IEnumerable<Attachment>> GetConversationAttachmentsAsync(int conversationId)
        {
            try
            {
                _logger.LogInformation("Fetching attachments for conversation {ConversationId}", conversationId);
 
                var attachments = await _context.Attachments
                    .Include(a => a.Message)
                    .Where(a => a.Message.ConversationId == conversationId && !a.Message.IsDeleted)
                    .OrderByDescending(a => a.UploadedAt)
                    .ToListAsync();
 
                return attachments;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching attachments for conversation {ConversationId}", conversationId);
                throw;
            }
        }
    }
}
