using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using GorevTakip.DataAccess.Repositories;
using GorevTakip.Entities;
using GorevTakip.Entities.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace GorevTakip.Business.Features.Tasks.Queries
{
    // Get Filtered Tasks
    public class GetFilteredTasksQuery : IRequest<PagedResponseDto<TaskResponseDto>>
    {
        public TaskFilterDto Filter { get; set; } = null!;
    }

    public class GetFilteredTasksQueryHandler : IRequestHandler<GetFilteredTasksQuery, PagedResponseDto<TaskResponseDto>>
    {
        private readonly ITaskRepository _taskRepository;
        private readonly IMapper _mapper;

        public GetFilteredTasksQueryHandler(ITaskRepository taskRepository, IMapper mapper)
        {
            _taskRepository = taskRepository;
            _mapper = mapper;
        }

        public async Task<PagedResponseDto<TaskResponseDto>> Handle(GetFilteredTasksQuery request, CancellationToken cancellationToken)
        {
            var filter = request.Filter;
            var (tasks, totalRecords) = await _taskRepository.GetFilteredTasksWithUsersAsync(filter);

            var mappedTasks = _mapper.Map<List<TaskResponseDto>>(tasks);
                
            return new PagedResponseDto<TaskResponseDto>
            {
                Data = mappedTasks,
                TotalRecords = totalRecords,
                TotalPages = (int)Math.Ceiling((double)totalRecords / filter.PageSize),
                CurrentPage = filter.PageNumber
            };
        }
    }

    // Get Task Statistics
    public class GetTaskStatisticsQuery : IRequest<TaskStatisticsDto>
    {
        public int? UserId { get; set; }
        public int? CategoryId { get; set; }
    }

    public class GetTaskStatisticsQueryHandler : IRequestHandler<GetTaskStatisticsQuery, TaskStatisticsDto>
    {
        private readonly ITaskRepository _taskRepository;
        private readonly IDistributedCache _cache;

        public GetTaskStatisticsQueryHandler(ITaskRepository taskRepository, IDistributedCache cache)
        {
            _taskRepository = taskRepository;
            _cache = cache;
        }

        public async Task<TaskStatisticsDto> Handle(GetTaskStatisticsQuery request, CancellationToken cancellationToken)
        {
            int? userId = request.UserId;
            int? categoryId = request.CategoryId;

            string cacheKey = $"TaskStats_User_{userId ?? 0}_Cat_{categoryId ?? 0}";
            
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

            var cacheOptions = new DistributedCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(10));

            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(stats), cacheOptions);

            return stats;
        }
    }

    // Get All Tasks
    public class GetAllTasksQuery : IRequest<IEnumerable<TaskResponseDto>>
    {
    }

    public class GetAllTasksQueryHandler : IRequestHandler<GetAllTasksQuery, IEnumerable<TaskResponseDto>>
    {
        private readonly ITaskRepository _taskRepository;
        private readonly IMapper _mapper;

        public GetAllTasksQueryHandler(ITaskRepository taskRepository, IMapper mapper)
        {
            _taskRepository = taskRepository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<TaskResponseDto>> Handle(GetAllTasksQuery request, CancellationToken cancellationToken)
        {
            var tasks = await _taskRepository.GetAllAsync();
            return _mapper.Map<IEnumerable<TaskResponseDto>>(tasks);
        }
    }

    // Get Task By Id
    public class GetTaskByIdQuery : IRequest<TaskResponseDto?>
    {
        public int Id { get; set; }
    }

    public class GetTaskByIdQueryHandler : IRequestHandler<GetTaskByIdQuery, TaskResponseDto?>
    {
        private readonly ITaskRepository _taskRepository;
        private readonly IMapper _mapper;

        public GetTaskByIdQueryHandler(ITaskRepository taskRepository, IMapper mapper)
        {
            _taskRepository = taskRepository;
            _mapper = mapper;
        }

        public async Task<TaskResponseDto?> Handle(GetTaskByIdQuery request, CancellationToken cancellationToken)
        {
            var task = await _taskRepository.GetByIdAsync(request.Id);
            if (task == null) return null;
            return _mapper.Map<TaskResponseDto>(task);
        }
    }

    // Get Tasks By User Id
    public class GetTasksByUserIdQuery : IRequest<IEnumerable<TaskResponseDto>>
    {
        public int UserId { get; set; }
    }

    public class GetTasksByUserIdQueryHandler : IRequestHandler<GetTasksByUserIdQuery, IEnumerable<TaskResponseDto>>
    {
        private readonly ITaskRepository _taskRepository;
        private readonly IMapper _mapper;

        public GetTasksByUserIdQueryHandler(ITaskRepository taskRepository, IMapper mapper)
        {
            _taskRepository = taskRepository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<TaskResponseDto>> Handle(GetTasksByUserIdQuery request, CancellationToken cancellationToken)
        {
            var userTasks = await _taskRepository.GetQueryable().Where(t => t.AssignedUserId == request.UserId).ToListAsync();
            return _mapper.Map<IEnumerable<TaskResponseDto>>(userTasks);
        }
    }

    // Get Task History
    public class GetTaskHistoryQuery : IRequest<IEnumerable<TaskHistoryDto>>
    {
        public int TaskId { get; set; }
    }

    public class GetTaskHistoryQueryHandler : IRequestHandler<GetTaskHistoryQuery, IEnumerable<TaskHistoryDto>>
    {
        private readonly ITaskHistoryRepository _historyRepository;

        public GetTaskHistoryQueryHandler(ITaskHistoryRepository historyRepository)
        {
            _historyRepository = historyRepository;
        }

        public async Task<IEnumerable<TaskHistoryDto>> Handle(GetTaskHistoryQuery request, CancellationToken cancellationToken)
        {
            var histories = await _historyRepository.GetHistoryByTaskIdAsync(request.TaskId);
            return histories.Select(h => new TaskHistoryDto
            {
                ActionMessage = h.ActionMessage,
                CreatedDate = h.CreatedDate
            });
        }
    }

    // Get Comments
    public class GetCommentsQuery : IRequest<IEnumerable<TaskCommentDto>>
    {
        public int TaskId { get; set; }
    }

    public class GetCommentsQueryHandler : IRequestHandler<GetCommentsQuery, IEnumerable<TaskCommentDto>>
    {
        private readonly ITaskCommentRepository _commentRepository;

        public GetCommentsQueryHandler(ITaskCommentRepository commentRepository)
        {
            _commentRepository = commentRepository;
        }

        public async Task<IEnumerable<TaskCommentDto>> Handle(GetCommentsQuery request, CancellationToken cancellationToken)
        {
            var comments = await _commentRepository.GetCommentsWithUserByTaskIdAsync(request.TaskId);
            
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
