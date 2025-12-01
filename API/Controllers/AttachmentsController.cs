using Core.Interfaces.IServices;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AttachmentsController : ControllerBase
    {
        private readonly IAttachmentService _attachmentService;
        private readonly ILogger<AttachmentsController> _logger;

        public AttachmentsController(IAttachmentService attachmentService, ILogger<AttachmentsController> logger)
        {
            _attachmentService = attachmentService;
            _logger = logger;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadAttachment(int messageId, IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest("File is required");

                var attachment = await _attachmentService.SaveAttachmentAsync(messageId, file);

                _logger.LogInformation("File uploaded for message {MessageId}", messageId);
                return Ok(attachment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpDelete("{attachmentId}")]
        public async Task<IActionResult> DeleteAttachment(int attachmentId)
        {
            try
            {
                var success = await _attachmentService.DeleteAttachmentAsync(attachmentId);

                if (!success)
                    return NotFound("Attachment not found");

                _logger.LogInformation("Attachment {AttachmentId} deleted", attachmentId);
                return Ok("Attachment deleted successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting attachment");
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }
}
