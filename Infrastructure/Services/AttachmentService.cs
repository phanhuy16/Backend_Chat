using Core.DTOs.Attachments;
using Core.Entities;
using Core.Interfaces.IRepositories;
using Core.Interfaces.IServices;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services
{
    public class AttachmentService : IAttachmentService
    {
        private readonly IAttachmentRepository _attachmentRepository;
        private readonly IMessageRepository _messageRepository;
        private readonly ILogger<AttachmentService> _logger;
        private readonly string _uploadsFolder;

        public AttachmentService(
            IAttachmentRepository attachmentRepository,
            IMessageRepository messageRepository,
            ILogger<AttachmentService> logger)
        {
            _attachmentRepository = attachmentRepository;
            _messageRepository = messageRepository;
            _logger = logger;
            _uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            if (!Directory.Exists(_uploadsFolder))
                Directory.CreateDirectory(_uploadsFolder);
        }

        public async Task<AttachmentDto> SaveAttachmentAsync(int messageId, IFormFile file)
        {
            try
            {
                // Validate file
                if (file == null || file.Length == 0)
                    throw new Exception("File is empty");

                if (file.Length > 50 * 1024 * 1024) // 50MB limit
                    throw new Exception("File size exceeds 50MB limit");

                var message = await _messageRepository.GetByIdAsync(messageId);
                if (message == null)
                    throw new Exception("Message not found");

                // Create uploads folder if not exists
                if (!Directory.Exists(_uploadsFolder))
                    Directory.CreateDirectory(_uploadsFolder);

                // Generate unique filename
                var fileName = $"{messageId}_{DateTime.UtcNow.Ticks}{Path.GetExtension(file.FileName)}";
                var filePath = Path.Combine(_uploadsFolder, fileName);

                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var fileUrl = $"/uploads/{fileName}";
                var fileType = Path.GetExtension(file.FileName).ToLowerInvariant();

                var attachment = new Attachment
                {
                    MessageId = messageId,
                    FileName = file.FileName,
                    FileUrl = fileUrl,
                    FileSize = file.Length,
                    FileType = fileType,
                    UploadedAt = DateTime.UtcNow
                };

                await _attachmentRepository.AddAsync(attachment);

                message.UpdatedAt = DateTime.UtcNow;
                await _messageRepository.UpdateAsync(message);

                _logger.LogInformation($"File {fileName} uploaded for message {messageId}");

                return new AttachmentDto
                {
                    Id = attachment.Id,
                    FileName = attachment.FileName,
                    FileUrl = attachment.FileUrl,
                    FileSize = attachment.FileSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error saving attachment: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> DeleteAttachmentAsync(int attachmentId)
        {
            try
            {
                var attachment = await _attachmentRepository.GetByIdAsync(attachmentId);
                if (attachment == null)
                    return false;

                // ✅ Delete file from disk
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", attachment.FileUrl.TrimStart('/'));
                if (File.Exists(filePath))
                    File.Delete(filePath);

                await _attachmentRepository.DeleteAsync(attachmentId);

                _logger.LogInformation($"Attachment {attachmentId} deleted");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting attachment: {ex.Message}");
                return false;
            }
        }
        public async Task<IEnumerable<AttachmentDto>> GetAttachmentsByConversationIdAsync(int conversationId)
        {
            try
            {
                var attachments = await _attachmentRepository.GetConversationAttachmentsAsync(conversationId);
                return attachments.Select(a => new AttachmentDto
                {
                    Id = a.Id,
                    FileName = a.FileName,
                    FileUrl = a.FileUrl,
                    FileSize = a.FileSize
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting attachments for conversation {conversationId}: {ex.Message}");
                throw;
            }
        }
    }
}
