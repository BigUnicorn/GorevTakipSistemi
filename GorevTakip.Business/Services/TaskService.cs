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
            var query = _taskRepository.GetQueryable().Include(t => t.AssignedUser).AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.SearchText))
            {
                var search = filter.SearchText.ToLower();
                query = query.Where(x => x.Title.ToLower().Contains(search) || x.Description.ToLower().Contains(search));
            }

            if (filter.Status.HasValue && filter.Status.Value > 0)
                query = query.Where(x => (int)x.Status == filter.Status.Value);

            if (filter.AssignedUserId.HasValue && filter.AssignedUserId.Value > 0)
                query = query.Where(x => x.AssignedUserId == filter.AssignedUserId.Value);

            var totalRecords = await query.CountAsync();

            if (!string.IsNullOrEmpty(filter.SortBy))
            {
                switch (filter.SortBy.ToLower())
                {
                    case "title":
                        query = filter.SortDescending ? query.OrderByDescending(x => x.Title) : query.OrderBy(x => x.Title);
                        break;
                    case "description":
                        query = filter.SortDescending ? query.OrderByDescending(x => x.Description) : query.OrderBy(x => x.Description);
                        break;
                    case "duedate":
                        query = filter.SortDescending ? query.OrderByDescending(x => x.DueDate) : query.OrderBy(x => x.DueDate);
                        break;
                    case "assigneduser":
                        query = filter.SortDescending
                             ? query.OrderByDescending(x => x.AssignedUser!.FirstName).ThenByDescending(x => x.AssignedUser!.LastName)
                             : query.OrderBy(x => x.AssignedUser!.FirstName).ThenBy(x => x.AssignedUser!.LastName);
                        break;
                    case "status":
                        query = filter.SortDescending ? query.OrderByDescending(x => x.Status) : query.OrderBy(x => x.Status);
                        break;
                    default:
                        query = query.OrderByDescending(x => x.DueDate);
                        break;
                }
            }
            else
            {
                query = query.OrderByDescending(x => x.DueDate);
            }

            var tasks = await query
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            var mappedTasks = _mapper.Map<List<TaskResponseDto>>(tasks);
                
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
            // 1. Cache Anahtarını oluştur (Versiyonlu)
            string cacheVersion = await GetCacheVersionAsync();
            string cacheKey = $"TaskStats_v{cacheVersion}_User_{userId ?? 0}_Cat_{categoryId ?? 0}";
            
            // 2. Redis'ten kontrol et
            var cachedDataString = await _cache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedDataString))
            {
                return JsonSerializer.Deserialize<TaskStatisticsDto>(cachedDataString)!;
            }

            var query = _taskRepository.GetQueryable();

            if (userId.HasValue && userId.Value > 0)
                query = query.Where(t => t.AssignedUserId == userId.Value);

            if (categoryId.HasValue && categoryId.Value > 0)
                query = query.Where(t => (int)t.Category == categoryId.Value);

            var stats = new TaskStatisticsDto
            {
                TotalTasks = await query.CountAsync(),
                TodoTasks = await query.CountAsync(t => t.Status == WorkStatus.Todo),
                InProgressTasks = await query.CountAsync(t => t.Status == WorkStatus.InProgress),
                CompletedTasks = await query.CountAsync(t => t.Status == WorkStatus.Done),
                FrontendTasks = await query.CountAsync(t => t.Category == TaskCategory.Frontend),
                BackendTasks = await query.CountAsync(t => t.Category == TaskCategory.Backend),
                DatabaseTasks = await query.CountAsync(t => t.Category == TaskCategory.Database),
                BugFixTasks = await query.CountAsync(t => t.Category == TaskCategory.BugFix),
                MobileTasks = await query.CountAsync(t => t.Category == TaskCategory.Mobile),
                DevOpsTasks = await query.CountAsync(t => t.Category == TaskCategory.DevOps)
            };

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

            await InvalidateTaskCacheAsync();
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
            await InvalidateTaskCacheAsync();
        }

        public async Task DeleteTaskAsync(int id)
        {
            var task = await _taskRepository.GetByIdAsync(id);
            if (task != null)
            {
                _taskRepository.Delete(task);
                await _unitOfWork.SaveChangesAsync();
                await InvalidateTaskCacheAsync();
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
            await InvalidateTaskCacheAsync();
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

        private async Task<string> GetCacheVersionAsync()
        {
            var version = await _cache.GetStringAsync("TaskCacheVersion");
            if (string.IsNullOrEmpty(version))
            {
                version = "1";
                await _cache.SetStringAsync("TaskCacheVersion", version);
            }
            return version;
        }

        private async Task InvalidateTaskCacheAsync()
        {
            var versionStr = await GetCacheVersionAsync();
            if (int.TryParse(versionStr, out int version))
            {
                // Versiyonu artırarak mevcut tüm cache'lerin (farklı filtreler dahil) geçersiz olmasını sağlıyoruz.
                await _cache.SetStringAsync("TaskCacheVersion", (version + 1).ToString());
            }
        }

    }
}