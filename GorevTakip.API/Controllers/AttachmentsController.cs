using System.Security.Claims;
using System.Threading.Tasks;
using GorevTakip.Business.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GorevTakip.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class AttachmentsController : ControllerBase
    {
        private readonly IAttachmentService _attachmentService;

        public AttachmentsController(IAttachmentService attachmentService)
        {
            _attachmentService = attachmentService;
        }

        // POST: api/attachments/task/5
        [HttpPost("task/{taskId}")]
        public async Task<IActionResult> UploadAttachment(int taskId, IFormFile file)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdStr, out int userId)) return Unauthorized();

            try
            {
                var attachment = await _attachmentService.UploadAttachmentAsync(taskId, userId, file);
                return Ok(attachment);
            }
            catch (System.Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        // GET: api/attachments/task/5
        [HttpGet("task/{taskId}")]
        public async Task<IActionResult> GetAttachments(int taskId)
        {
            var attachments = await _attachmentService.GetAttachmentsByTaskIdAsync(taskId);
            return Ok(attachments);
        }

        // GET: api/attachments/5/download
        [HttpGet("{id}/download")]
        [AllowAnonymous] // Tarayıcıdan doğrudan indirme yapılabilmesi için geçici izin verebiliriz veya URL'e token ekletebiliriz.
        public async Task<IActionResult> DownloadAttachment(int id)
        {
            try
            {
                var (fileBytes, contentType, fileName) = await _attachmentService.DownloadAttachmentAsync(id);
                return File(fileBytes, contentType, fileName);
            }
            catch (System.Exception ex)
            {
                return NotFound(ex.Message);
            }
        }

        // DELETE: api/attachments/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAttachment(int id)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            if (!int.TryParse(userIdStr, out int userId)) return Unauthorized();

            try
            {
                await _attachmentService.DeleteAttachmentAsync(id, userId, role ?? "");
                return Ok(new { Message = "Dosya başarıyla silindi." });
            }
            catch (System.Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}
