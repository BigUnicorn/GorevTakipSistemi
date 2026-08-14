using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using GorevTakip.DataAccess.Repositories;
using GorevTakip.Entities;
using GorevTakip.Entities.DTOs;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using Microsoft.Extensions.Caching.Distributed;

namespace GorevTakip.Business.Services
{
    public class TaskService : ITaskService
    {
        private readonly ITaskRepository _taskRepository;
        private readonly IGenericRepository<User> _userRepository; 
        private readonly ITaskHistoryRepository _historyRepository;
        private readonly ITaskCommentRepository _commentRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IDistributedCache _cache;

        // Güncellenmiş Constructor:
        public TaskService(
            ITaskRepository taskRepository,          // <-- GÜNCELLENDİ
            IGenericRepository<User> userRepository, 
            ITaskHistoryRepository historyRepository, // <-- GÜNCELLENDİ
            ITaskCommentRepository commentRepository, // <-- GÜNCELLENDİ
            IMapper mapper,
            IUnitOfWork unitOfWork,
            IDistributedCache cache)
        {
            _taskRepository = taskRepository;
            _userRepository = userRepository;
            _historyRepository = historyRepository;
            _commentRepository = commentRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _cache = cache;
        }

        public async Task<PagedResponseDto<TaskResponseDto>> GetFilteredTasksAsync(TaskFilterDto filter)
        {
            // 1. Repository'den veriyi ve sayıyı al (Veritabanı işlemleri gizlendi)
            var (tasks, totalRecords) = await _taskRepository.GetFilteredTasksAsync(filter);

            // 2. DTO'ya dönüştür
            var mappedTasks = _mapper.Map<List<TaskResponseDto>>(tasks);
                
            // 3. İstemciye (Frontend) gönder
            return new PagedResponseDto<TaskResponseDto>
            {
                Data = mappedTasks,
                TotalRecords = totalRecords,
                TotalPages = (int)Math.Ceiling((double)totalRecords / filter.PageSize),
                CurrentPage = filter.PageNumber
            };
        }

        public async Task<TaskStatisticsDto> GetTaskStatisticsAsync(int? userId = null, int? categoryId = null)
        {
            // 1. Cache Anahtarını oluştur
            string cacheKey = $"TaskStats_User_{userId ?? 0}_Cat_{categoryId ?? 0}";
            
            // 2. Redis'ten kontrol et
            var cachedDataString = await _cache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedDataString))
            {
                return JsonSerializer.Deserialize<TaskStatisticsDto>(cachedDataString)!;
            }

            // 3. EĞER CACHE'DE YOKSA: Veritabanı işlemini Repository'e devret!
            // (Aşağıdaki tek satır, eskiden burada olan 15 satırlık IQueryable ve CountAsync yığınının yerini aldı)
            var stats = await _taskRepository.GetTaskStatisticsAsync(userId, categoryId);

            // 4. Redis'e kaydet
            var cacheOptions = new DistributedCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(1));

            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(stats), cacheOptions);

            return stats;
        }

        public async Task<IEnumerable<TaskResponseDto>> GetAllTasksAsync()
        {
            var tasks = await _taskRepository.GetAllAsync();
            // DEĞİŞİKLİK 2: MapToResponseDto yerine Mapper kullanıldı
            return _mapper.Map<IEnumerable<TaskResponseDto>>(tasks);
        }

        public async Task<TaskResponseDto?> GetTaskByIdAsync(int id)
        {
            var task = await _taskRepository.GetByIdAsync(id);
            if (task == null) return null;
            // DEĞİŞİKLİK 3: MapToResponseDto yerine Mapper kullanıldı
            return _mapper.Map<TaskResponseDto>(task);
        }

        public async Task<IEnumerable<TaskResponseDto>> GetTasksByUserIdAsync(int userId)
        {
            var tasks = await _taskRepository.GetAllAsync();
            var userTasks = tasks.Where(t => t.AssignedUserId == userId);
            // DEĞİŞİKLİK 4: MapToResponseDto yerine Mapper kullanıldı
            return _mapper.Map<IEnumerable<TaskResponseDto>>(userTasks);
        }

        public async Task<TaskResponseDto> CreateTaskAsync(TaskCreateDto taskDto)
        {
            var userExists = await _userRepository.GetByIdAsync(taskDto.AssignedUserId);
            if (userExists == null)
                throw new Exception("Atanan kullanıcı bulunamadı!");

            // DTO'dan Entity'e çeviri
            var taskItem = _mapper.Map<TaskItem>(taskDto);

            await _taskRepository.AddAsync(taskItem);

            // ÇÖZÜM BURADA: TaskId = taskItem.Id YERİNE, doğrudan objeyi (Task = taskItem) veriyoruz.
            var history = new TaskHistory 
            { 
                Task = taskItem, // EF Core bu ilişkiyi algılayıp ID atamasını otomatik yapacak!
                ActionMessage = "Görev oluşturuldu." 
            };
            
            await _historyRepository.AddAsync(history);
            
            // Her iki işlem de tek bir Transaction (işlem) olarak veritabanına sorunsuzca yansıtılacak.
            await _unitOfWork.SaveChangesAsync();

            InvalidateTaskCache(taskItem.AssignedUserId, (int)taskItem.Category);
            return _mapper.Map<TaskResponseDto>(taskItem);
        }

        public async Task UpdateTaskAsync(TaskUpdateDto taskDto)
        {
            var existingTask = await _taskRepository.GetByIdAsync(taskDto.Id);
            if (existingTask == null)
                throw new Exception("Güncellenecek görev bulunamadı.");

            var userExists = await _userRepository.GetByIdAsync(taskDto.AssignedUserId);
            if (userExists == null)
                throw new Exception("Atanan kullanıcı bulunamadı!");

            existingTask.Title = taskDto.Title;
            existingTask.Description = taskDto.Description;
            existingTask.Status = taskDto.Status;
            existingTask.DueDate = taskDto.DueDate;
            existingTask.AssignedUserId = taskDto.AssignedUserId;
            existingTask.Category = taskDto.Category;

            _taskRepository.Update(existingTask);
            
            var history = new TaskHistory 
            { 
                TaskId = existingTask.Id, 
                ActionMessage = "Görevin detayları güncellendi." 
            };
            await _historyRepository.AddAsync(history);

            await _unitOfWork.SaveChangesAsync();
            InvalidateTaskCache(existingTask.AssignedUserId, (int)existingTask.Category);
        }

        public async Task DeleteTaskAsync(int id)
        {
            var task = await _taskRepository.GetByIdAsync(id);
            if (task != null)
            {
                _taskRepository.Delete(task);
                InvalidateTaskCache(task.AssignedUserId, (int)task.Category);
                await _unitOfWork.SaveChangesAsync();
            }
        }

        public async Task UpdateTaskStatusAsync(int id, WorkStatus newStatus)
        {
            var task = await _taskRepository.GetByIdAsync(id);
            if (task == null)
                throw new Exception("Görev bulunamadı.");

            task.Status = newStatus;
            _taskRepository.Update(task);

            var history = new TaskHistory 
            { 
                TaskId = task.Id, 
                ActionMessage = $"Görev durumu güncellendi: {newStatus}" 
            };
            await _historyRepository.AddAsync(history);

            await _unitOfWork.SaveChangesAsync();
            InvalidateTaskCache(task.AssignedUserId, (int)task.Category);
        }

        public async Task<IEnumerable<TaskHistoryDto>> GetTaskHistoryAsync(int taskId)
        {
            // 1. Veritabanı sorgulama mantığını tamamen Repository'ye devrettik.
            // Artık Business katmanı "Where" veya "OrderByDescending" gibi EF Core komutlarını bilmiyor.
            var histories = await _historyRepository.GetHistoryByTaskIdAsync(taskId);

            // 2. Gelen saf veriyi DTO'ya dönüştürüp döndürüyoruz.
            return histories.Select(h => new TaskHistoryDto
            {
                ActionMessage = h.ActionMessage,
                CreatedDate = h.CreatedDate
            });
        }

        public async Task AddCommentAsync(int taskId, int userId, string text)
        {
            var comment = new TaskComment
            {
                TaskId = taskId,
                UserId = userId,
                Text = text,
                CreatedDate = DateTime.UtcNow
            };
            await _commentRepository.AddAsync(comment);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<IEnumerable<TaskCommentDto>> GetCommentsAsync(int taskId)
        {
            // EF Core'un "Include" (Join işlemi) mantığını Business katmanından gizledik.
            var comments = await _commentRepository.GetCommentsWithUserByTaskIdAsync(taskId);
            
            return comments.Select(c => new TaskCommentDto
            {
                Id = c.Id,
                Text = c.Text,
                UserName = c.User != null ? $"{c.User.FirstName} {c.User.LastName}" : "Bilinmiyor",
                CreatedDate = c.CreatedDate
            });
        }

        private void InvalidateTaskCache(int? userId = null, int? categoryId = null)
        {
            // Genel (sistemdeki herkesin) istatistiklerini temizle
            _cache.Remove("TaskStats_User_0_Cat_0");

            // Eğer belirli bir kullanıcıya ait işlem yapıldıysa onun cache'ini temizle
            if (userId.HasValue)
            {
                _cache.Remove($"TaskStats_User_{userId.Value}_Cat_0");
            }

            // Kategoriye özel istatistikleri temizle
            if (categoryId.HasValue)
            {
                _cache.Remove($"TaskStats_User_0_Cat_{categoryId.Value}");
            }

            // Hem kullanıcı hem kategori bazlı çapraz filtreleri temizle
            if (userId.HasValue && categoryId.HasValue)
            {
                _cache.Remove($"TaskStats_User_{userId.Value}_Cat_{categoryId.Value}");
            }
        }

    }
}