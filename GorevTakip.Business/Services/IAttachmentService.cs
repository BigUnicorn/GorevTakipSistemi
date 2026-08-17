using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using GorevTakip.Business.DTOs;
using Microsoft.AspNetCore.Http;

namespace GorevTakip.Business.Services
{
    public interface IAttachmentService
    {
        Task<TaskAttachmentDto> UploadAttachmentAsync(int taskId, int userId, IFormFile file);
        Task<IEnumerable<TaskAttachmentDto>> GetAttachmentsByTaskIdAsync(int taskId);
        Task<TaskAttachmentDto?> GetAttachmentByIdAsync(int attachmentId);
        Task<(byte[] FileBytes, string ContentType, string FileName)> DownloadAttachmentAsync(int attachmentId);
        Task DeleteAttachmentAsync(int attachmentId, int userId, string role);
    }
}
