using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GorevTakip.Business.DTOs;
using GorevTakip.DataAccess.Repositories;
using GorevTakip.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using GorevTakip.Business.Exceptions;

namespace GorevTakip.Business.Services
{
    public class AttachmentService : IAttachmentService
    {
        private readonly ITaskAttachmentRepository _attachmentRepository;
        private readonly ITaskRepository _taskRepository;
        private readonly IGenericRepository<User> _userRepository;
        private readonly ITaskHistoryRepository _historyRepository;
        private readonly IUnitOfWork _unitOfWork;

        // Dosyaların kaydedileceği klasör
        private readonly string _uploadDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");

        public AttachmentService(
            ITaskAttachmentRepository attachmentRepository,
            ITaskRepository taskRepository,
            IGenericRepository<User> userRepository,
            ITaskHistoryRepository historyRepository,
            IUnitOfWork unitOfWork)
        {
            _attachmentRepository = attachmentRepository;
            _taskRepository = taskRepository;
            _userRepository = userRepository;
            _historyRepository = historyRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<TaskAttachmentDto> UploadAttachmentAsync(int taskId, int userId, IFormFile file)
        {
            var task = await _taskRepository.GetByIdAsync(taskId);
            if (task == null) throw new NotFoundException("Görev bulunamadı.");

            if (file == null || file.Length == 0)
                throw new BadRequestException("Geçerli bir dosya yüklemediniz.");

            if (file.Length > 10 * 1024 * 1024) // 10 MB limit
                throw new BadRequestException("Dosya boyutu 10MB'dan büyük olamaz.");

            // Güvenlik: Sadece izin verilen dosya uzantıları
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".txt" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            
            if (string.IsNullOrEmpty(extension) || !allowedExtensions.Contains(extension))
            {
                throw new BadRequestException("Bu dosya türünün yüklenmesine izin verilmiyor. Sadece resim, PDF, Office ve metin dosyaları yüklenebilir.");
            }

            // Klasör yoksa oluştur
            if (!Directory.Exists(_uploadDirectory))
            {
                Directory.CreateDirectory(_uploadDirectory);
            }

            // Güvenli dosya adı oluşturma (Guid ekleyerek çakışmaları önleme)
            var safeFileName = $"{Guid.NewGuid()}{extension}";
            var physicalPath = Path.Combine(_uploadDirectory, safeFileName);
            var relativePath = $"/uploads/{safeFileName}"; // Web üzerinden erişilecek yol

            // Fiziksel diske kaydet
            using (var stream = new FileStream(physicalPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // DB'ye kaydet
            var attachment = new TaskAttachment
            {
                TaskId = taskId,
                FileName = file.FileName, // Orijinal adı koruyoruz
                FilePath = relativePath,  // İndirmek için kullanılacak URL yolu
                ContentType = file.ContentType,
                FileSize = file.Length,
                UploadedByUserId = userId,
                UploadedAt = DateTime.UtcNow
            };

            await _attachmentRepository.AddAsync(attachment);

            // Geçmişe ekle
            var history = new TaskHistory
            {
                TaskId = taskId,
                ActionMessage = $"'{file.FileName}' adlı dosya eklendi."
            };
            await _historyRepository.AddAsync(history);

            await _unitOfWork.SaveChangesAsync();

            return await MapToDtoAsync(attachment);
        }

        public async Task<IEnumerable<TaskAttachmentDto>> GetAttachmentsByTaskIdAsync(int taskId)
        {
            var attachments = await _attachmentRepository.GetQueryable()
                                .Where(a => a.TaskId == taskId)
                                .ToListAsync();
            
            var dtos = new List<TaskAttachmentDto>();
            foreach(var att in attachments.OrderByDescending(a => a.UploadedAt))
            {
                dtos.Add(await MapToDtoAsync(att));
            }
            return dtos;
        }

        public async Task<TaskAttachmentDto?> GetAttachmentByIdAsync(int attachmentId)
        {
            var attachment = await _attachmentRepository.GetByIdAsync(attachmentId);
            if (attachment == null) return null;
            return await MapToDtoAsync(attachment);
        }

        public async Task<(byte[] FileBytes, string ContentType, string FileName)> DownloadAttachmentAsync(int attachmentId)
        {
            var attachment = await _attachmentRepository.GetByIdAsync(attachmentId);
            if (attachment == null) throw new NotFoundException("Dosya bulunamadı.");

            var physicalPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", attachment.FilePath.TrimStart('/'));

            if (!File.Exists(physicalPath))
                throw new NotFoundException("Dosya sunucuda bulunamadı.");

            var memory = new MemoryStream();
            using (var stream = new FileStream(physicalPath, FileMode.Open))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;

            return (memory.ToArray(), attachment.ContentType, attachment.FileName);
        }

        public async Task DeleteAttachmentAsync(int attachmentId, int userId, string role)
        {
            var attachment = await _attachmentRepository.GetByIdAsync(attachmentId);
            if (attachment == null) throw new NotFoundException("Dosya bulunamadı.");

            var task = await _taskRepository.GetByIdAsync(attachment.TaskId);

            // Sadece Admin, dosyayı yükleyen kişi VEYA görevin sahibi silebilir
            if (role != nameof(UserRole.Admin) && attachment.UploadedByUserId != userId && (task == null || task.AssignedUserId != userId))
            {
                throw new UnauthorizedActionException("Bu dosyayı silme yetkiniz yok.");
            }

            // Fiziksel diskten sil
            var physicalPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", attachment.FilePath.TrimStart('/'));
            if (File.Exists(physicalPath))
            {
                File.Delete(physicalPath);
            }

            // DB'den sil
            _attachmentRepository.Delete(attachment);

            // Geçmişe ekle
            var history = new TaskHistory
            {
                TaskId = attachment.TaskId,
                ActionMessage = $"'{attachment.FileName}' adlı dosya silindi."
            };
            await _historyRepository.AddAsync(history);

            await _unitOfWork.SaveChangesAsync();
        }

        private async Task<TaskAttachmentDto> MapToDtoAsync(TaskAttachment attachment)
        {
            var user = await _userRepository.GetByIdAsync(attachment.UploadedByUserId);
            return new TaskAttachmentDto
            {
                Id = attachment.Id,
                TaskId = attachment.TaskId,
                FileName = attachment.FileName,
                FilePath = attachment.FilePath,
                ContentType = attachment.ContentType,
                FileSize = attachment.FileSize,
                UploadedAt = attachment.UploadedAt,
                UploadedByUserId = attachment.UploadedByUserId,
                UploadedByUserName = user != null ? $"{user.FirstName} {user.LastName}" : "Bilinmeyen"
            };
        }
    }
}
