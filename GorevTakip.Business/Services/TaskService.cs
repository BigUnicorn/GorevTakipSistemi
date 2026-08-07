using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GorevTakip.DataAccess.Repositories;
using GorevTakip.Entities;
using GorevTakip.Entities.DTOs;
using Microsoft.EntityFrameworkCore;

namespace GorevTakip.Business.Services
{
    public class TaskService : ITaskService
    {
        private readonly IGenericRepository<TaskItem> _taskRepository;
        private readonly IGenericRepository<User> _userRepository;
        
        // 1. GÖREV GEÇMİŞİ İÇİN REPOSITORY EKLENDİ
        private readonly IGenericRepository<TaskHistory> _historyRepository;

        // 2. CONSTRUCTOR GÜNCELLENDİ (historyRepository eklendi)
        public TaskService(
            IGenericRepository<TaskItem> taskRepository, 
            IGenericRepository<User> userRepository,
            IGenericRepository<TaskHistory> historyRepository)
        {
            _taskRepository = taskRepository;
            _userRepository = userRepository;
            _historyRepository = historyRepository;
        }

        public async Task<PagedResponseDto<TaskResponseDto>> GetFilteredTasksAsync(TaskFilterDto filter)
        {
            IQueryable<TaskItem> query = _taskRepository.GetQueryable().Include(t => t.AssignedUser);

            if (!string.IsNullOrWhiteSpace(filter.SearchText))
            {
                var search = filter.SearchText.ToLower();
                query = query.Where(x => x.Title.ToLower().Contains(search) ||
                                        x.Description.ToLower().Contains(search));
            }

            if (filter.Status.HasValue && filter.Status.Value > 0)
            {
                query = query.Where(x => (int)x.Status == filter.Status.Value);
            }

            if (filter.AssignedUserId.HasValue && filter.AssignedUserId.Value > 0)
            {
                query = query.Where(x => x.AssignedUserId == filter.AssignedUserId.Value);
            }

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

            var mappedTasks = tasks.Select(t => new TaskResponseDto
                {
                    Id = t.Id,
                    Title = t.Title,
                    Description = t.Description,
                    Status = t.Status,
                    DueDate = t.DueDate,
                    AssignedUserId = t.AssignedUserId,
                    Category = t.Category,
                    AssignedUserName = t.AssignedUser != null ? $"{t.AssignedUser.FirstName} {t.AssignedUser.LastName}" : "Bilinmiyor"
                }).ToList();
                
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
            var query = _taskRepository.GetQueryable();

            if (userId.HasValue && userId.Value > 0)
            {
                query = query.Where(t => t.AssignedUserId == userId.Value);
            }

            if (categoryId.HasValue && categoryId.Value > 0)
            {
                query = query.Where(t => (int)t.Category == categoryId.Value);
            }

            var total = await query.CountAsync();
            var todo = await query.CountAsync(t => t.Status == WorkStatus.Todo);
            var inProgress = await query.CountAsync(t => t.Status == WorkStatus.InProgress);
            var completed = await query.CountAsync(t => t.Status == WorkStatus.Done);

            var frontend = await query.CountAsync(t => t.Category == TaskCategory.Frontend);
            var backend = await query.CountAsync(t => t.Category == TaskCategory.Backend);
            var database = await query.CountAsync(t => t.Category == TaskCategory.Database);
            var bugFix = await query.CountAsync(t => t.Category == TaskCategory.BugFix);
            var mobile = await query.CountAsync(t => t.Category == TaskCategory.Mobile);
            var devOps = await query.CountAsync(t => t.Category == TaskCategory.DevOps);

            return new TaskStatisticsDto
            {
                TotalTasks = total,
                TodoTasks = todo,
                InProgressTasks = inProgress,
                CompletedTasks = completed,

                FrontendTasks = frontend,
                BackendTasks = backend,
                DatabaseTasks = database,
                BugFixTasks = bugFix,
                MobileTasks = mobile,
                DevOpsTasks = devOps
            };
        }

        public async Task<IEnumerable<TaskResponseDto>> GetAllTasksAsync()
        {
            var tasks = await _taskRepository.GetAllAsync();
            return tasks.Select(MapToResponseDto);
        }

        public async Task<TaskResponseDto?> GetTaskByIdAsync(int id)
        {
            var task = await _taskRepository.GetByIdAsync(id);
            if (task == null) return null;
            return MapToResponseDto(task);
        }

        public async Task<IEnumerable<TaskResponseDto>> GetTasksByUserIdAsync(int userId)
        {
            var tasks = await _taskRepository.GetAllAsync();
            return tasks.Where(t => t.AssignedUserId == userId).Select(MapToResponseDto);
        }

        public async Task<TaskResponseDto> CreateTaskAsync(TaskCreateDto taskDto)
        {
            var userExists = await _userRepository.GetByIdAsync(taskDto.AssignedUserId);
            if (userExists == null)
                throw new Exception("Atanan kullanıcı bulunamadı!");

            var taskItem = new TaskItem
            {
                Title = taskDto.Title,
                Description = taskDto.Description,
                DueDate = taskDto.DueDate,
                AssignedUserId = taskDto.AssignedUserId,
                Status = WorkStatus.Todo,
                Category = taskDto.Category,
                CreatedDate = DateTime.UtcNow
            };

            await _taskRepository.AddAsync(taskItem);
            await _taskRepository.SaveChangesAsync();

            // 3. OLUŞTURMA İŞLEMİNİ LOGLAMA EKLENDİ
            var history = new TaskHistory 
            { 
                TaskId = taskItem.Id, 
                ActionMessage = "Görev oluşturuldu." 
            };
            await _historyRepository.AddAsync(history);
            await _historyRepository.SaveChangesAsync();

            return MapToResponseDto(taskItem);
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
            
            // 4. GÜNCELLEME İŞLEMİNİ LOGLAMA EKLENDİ
            var history = new TaskHistory 
            { 
                TaskId = existingTask.Id, 
                ActionMessage = "Görevin detayları güncellendi." 
            };
            await _historyRepository.AddAsync(history);

            await _taskRepository.SaveChangesAsync();
        }

        public async Task DeleteTaskAsync(int id)
        {
            var task = await _taskRepository.GetByIdAsync(id);
            if (task != null)
            {
                _taskRepository.Delete(task);
                await _taskRepository.SaveChangesAsync();
            }
        }

        public async Task UpdateTaskStatusAsync(int id, WorkStatus newStatus)
        {
            var task = await _taskRepository.GetByIdAsync(id);
            if (task == null)
                throw new Exception("Görev bulunamadı.");

            task.Status = newStatus;
            _taskRepository.Update(task);

            // 5. DURUM DEĞİŞİKLİĞİNİ LOGLAMA EKLENDİ (Hazır el atmışken buraya da ekledim)
            var history = new TaskHistory 
            { 
                TaskId = task.Id, 
                ActionMessage = $"Görev durumu güncellendi: {newStatus}" 
            };
            await _historyRepository.AddAsync(history);

            await _taskRepository.SaveChangesAsync();
        }

        // 6. GÖREV GEÇMİŞİNİ GETİRME METODU EKLENDİ
        public async Task<IEnumerable<TaskHistoryDto>> GetTaskHistoryAsync(int taskId)
        {
            var histories = await _historyRepository.GetQueryable()
                .Where(h => h.TaskId == taskId)
                .OrderByDescending(h => h.CreatedDate)
                .ToListAsync();

            return histories.Select(h => new TaskHistoryDto
            {
                ActionMessage = h.ActionMessage,
                CreatedDate = h.CreatedDate
            });
        }

        private TaskResponseDto MapToResponseDto(TaskItem task)
        {
            return new TaskResponseDto
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Description,
                Status = task.Status,
                CreatedDate = task.CreatedDate,
                DueDate = task.DueDate,
                AssignedUserId = task.AssignedUserId,
                Category = task.Category,
                AssignedUserName = task.AssignedUser != null ? $"{task.AssignedUser.FirstName} {task.AssignedUser.LastName}" : "Bilinmiyor" 
            };
        }
    }
}