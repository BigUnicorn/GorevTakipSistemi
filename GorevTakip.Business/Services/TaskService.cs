using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GorevTakip.DataAccess.Repositories;
using GorevTakip.Entities;
using GorevTakip.Entities.DTOs;
using Microsoft.EntityFrameworkCore;
using AutoMapper;

namespace GorevTakip.Business.Services
{
    public class TaskService : ITaskService
    {
        private readonly IGenericRepository<TaskItem> _taskRepository;
        private readonly IGenericRepository<User> _userRepository;
        private readonly IGenericRepository<TaskHistory> _historyRepository;
        private readonly IGenericRepository<TaskComment> _commentRepository;
        private readonly IMapper _mapper;

        public TaskService(
            IGenericRepository<TaskItem> taskRepository, 
            IGenericRepository<User> userRepository,
            IGenericRepository<TaskHistory> historyRepository,
            IGenericRepository<TaskComment> commentRepository,
            IMapper mapper)
        {
            _taskRepository = taskRepository;
            _userRepository = userRepository;
            _historyRepository = historyRepository;
            _commentRepository = commentRepository;
            _mapper = mapper;
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

            // DEĞİŞİKLİK 1: Buradaki uzun "tasks.Select(t => new TaskResponseDto...)" kodunu sildik. 
            // Yerine AutoMapper'ın tek satırlık çeviri kodunu yazdık.
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

            // DEĞİŞİKLİK 5: "var taskItem = new TaskItem { ... }" bloğu silinip tek satıra düşürüldü.
            var taskItem = _mapper.Map<TaskItem>(taskDto);

            await _taskRepository.AddAsync(taskItem);
            await _taskRepository.SaveChangesAsync();

            var history = new TaskHistory 
            { 
                TaskId = taskItem.Id, 
                ActionMessage = "Görev oluşturuldu." 
            };
            await _historyRepository.AddAsync(history);
            await _historyRepository.SaveChangesAsync();

            // DEĞİŞİKLİK 6: MapToResponseDto yerine Mapper kullanıldı
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

            var history = new TaskHistory 
            { 
                TaskId = task.Id, 
                ActionMessage = $"Görev durumu güncellendi: {newStatus}" 
            };
            await _historyRepository.AddAsync(history);

            await _taskRepository.SaveChangesAsync();
        }

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
            await _commentRepository.SaveChangesAsync();
        }

        public async Task<IEnumerable<TaskCommentDto>> GetCommentsAsync(int taskId)
        {
            var comments = await _commentRepository.GetQueryable()
                .Include(c => c.User)
                .Where(c => c.TaskId == taskId)
                .OrderBy(c => c.CreatedDate)
                .ToListAsync();

            return comments.Select(c => new TaskCommentDto
            {
                Id = c.Id,
                Text = c.Text,
                UserName = c.User != null ? $"{c.User.FirstName} {c.User.LastName}" : "Bilinmiyor",
                CreatedDate = c.CreatedDate
            });
        }

    }
}