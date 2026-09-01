using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using GorevTakip.DataAccess.Repositories;
using GorevTakip.Entities;
using GorevTakip.Entities.DTOs;
using System.Text.Json;
using GorevTakip.Business.Services;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using GorevTakip.Business.Exceptions;

namespace GorevTakip.Business.Features.Tasks.Commands
{
    // Create Task
    public class CreateTaskCommand : IRequest<TaskResponseDto>
    {
        public TaskCreateDto TaskDto { get; set; } = null!;
    }

    public class CreateTaskCommandHandler : IRequestHandler<CreateTaskCommand, TaskResponseDto>
    {
        private readonly ITaskRepository _taskRepository;
        private readonly IGenericRepository<User> _userRepository;
        private readonly ITaskHistoryRepository _historyRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IDistributedCache _cache;
        private readonly IOutboxRepository _outboxRepository;
        private readonly INotificationService _notificationService;

        public CreateTaskCommandHandler(ITaskRepository taskRepository, IGenericRepository<User> userRepository, 
            ITaskHistoryRepository historyRepository, IUnitOfWork unitOfWork, IMapper mapper, IDistributedCache cache, IOutboxRepository outboxRepository, INotificationService notificationService)
        {
            _taskRepository = taskRepository;
            _userRepository = userRepository;
            _historyRepository = historyRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _cache = cache;
            _outboxRepository = outboxRepository;
            _notificationService = notificationService;
        }

        public async Task<TaskResponseDto> Handle(CreateTaskCommand request, CancellationToken cancellationToken)
        {
            var taskDto = request.TaskDto;
            var userExists = await _userRepository.GetByIdAsync(taskDto.AssignedUserId);
            if (userExists == null)
                throw new NotFoundException("Atanan kullanıcı bulunamadı!");

            var taskItem = _mapper.Map<TaskItem>(taskDto);

            await _taskRepository.AddAsync(taskItem);

            var history = new TaskHistory 
            { 
                Task = taskItem,
                ActionMessage = "Görev oluşturuldu." 
            };
            
            await _historyRepository.AddAsync(history);
            
            var responseDto = _mapper.Map<TaskResponseDto>(taskItem);
            var userName = $"{userExists.FirstName} {userExists.LastName}";
            await _notificationService.CreateNotificationsAsync($"{userName} kullanıcısına yeni bir görev atandı: '{taskItem.Title}'", taskItem.AssignedUserId, taskItem.Id);
            var outboxMessage = new OutboxMessage
            {
                Type = "ReceiveTaskUpdate",
                Payload = JsonSerializer.Serialize(new { Action = "Create", Task = responseDto })
            };
            await _outboxRepository.AddAsync(outboxMessage);
            // Notification injection handled dynamically
            
            await _unitOfWork.SaveChangesAsync();

            await InvalidateTaskCacheAsync(taskItem.AssignedUserId, (int)taskItem.Category);
            return responseDto;
        }

        private async Task InvalidateTaskCacheAsync(int? userId = null, int? categoryId = null)
        {
            await _cache.RemoveAsync("TaskStats_User_0_Cat_0");
            if (userId.HasValue) await _cache.RemoveAsync($"TaskStats_User_{userId.Value}_Cat_0");
            if (categoryId.HasValue) await _cache.RemoveAsync($"TaskStats_User_0_Cat_{categoryId.Value}");
            if (userId.HasValue && categoryId.HasValue) await _cache.RemoveAsync($"TaskStats_User_{userId.Value}_Cat_{categoryId.Value}");
        }
    }

    // Update Task
    public class UpdateTaskCommand : IRequest
    {
        public TaskUpdateDto TaskDto { get; set; } = null!;
    }

    public class UpdateTaskCommandHandler : IRequestHandler<UpdateTaskCommand>
    {
        private readonly ITaskRepository _taskRepository;
        private readonly IGenericRepository<User> _userRepository;
        private readonly ITaskHistoryRepository _historyRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IDistributedCache _cache;
        private readonly IOutboxRepository _outboxRepository;
        private readonly INotificationService _notificationService;
        private readonly IMapper _mapper;

        public UpdateTaskCommandHandler(ITaskRepository taskRepository, IGenericRepository<User> userRepository, 
            ITaskHistoryRepository historyRepository, IUnitOfWork unitOfWork, IDistributedCache cache, IOutboxRepository outboxRepository, INotificationService notificationService, IMapper mapper)
        {
            _taskRepository = taskRepository;
            _userRepository = userRepository;
            _historyRepository = historyRepository;
            _unitOfWork = unitOfWork;
            _cache = cache;
            _outboxRepository = outboxRepository;
            _notificationService = notificationService;
            _mapper = mapper;
        }

        public async Task Handle(UpdateTaskCommand request, CancellationToken cancellationToken)
        {
            var taskDto = request.TaskDto;
            var existingTask = await _taskRepository.GetByIdAsync(taskDto.Id);
            if (existingTask == null) throw new NotFoundException("Güncellenecek görev bulunamadı.");

            var userExists = await _userRepository.GetByIdAsync(taskDto.AssignedUserId);
            if (userExists == null) throw new NotFoundException("Atanan kullanıcı bulunamadı!");

            int oldUserId = existingTask.AssignedUserId;
            int oldCategoryId = (int)existingTask.Category;

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
            
            var userName = $"{userExists.FirstName} {userExists.LastName}";
            await _notificationService.CreateNotificationsAsync($"{userName} kullanıcısının '{existingTask.Title}' adlı görevi güncellendi.", existingTask.AssignedUserId, existingTask.Id);
            var outboxMessage = new OutboxMessage
            {
                Type = "ReceiveTaskUpdate",
                Payload = JsonSerializer.Serialize(new { Action = "Update", Task = _mapper.Map<TaskResponseDto>(existingTask) })
            };
            await _outboxRepository.AddAsync(outboxMessage);
            // Notification injection handled dynamically

            await _unitOfWork.SaveChangesAsync();

            await InvalidateTaskCacheAsync(oldUserId, oldCategoryId);
            await InvalidateTaskCacheAsync(taskDto.AssignedUserId, (int)taskDto.Category);
        }

        private async Task InvalidateTaskCacheAsync(int? userId = null, int? categoryId = null)
        {
            await _cache.RemoveAsync("TaskStats_User_0_Cat_0");
            if (userId.HasValue) await _cache.RemoveAsync($"TaskStats_User_{userId.Value}_Cat_0");
            if (categoryId.HasValue) await _cache.RemoveAsync($"TaskStats_User_0_Cat_{categoryId.Value}");
            if (userId.HasValue && categoryId.HasValue) await _cache.RemoveAsync($"TaskStats_User_{userId.Value}_Cat_{categoryId.Value}");
        }
    }

    // Delete Task
    public class DeleteTaskCommand : IRequest
    {
        public int Id { get; set; }
    }

    public class DeleteTaskCommandHandler : IRequestHandler<DeleteTaskCommand>
    {
        private readonly ITaskRepository _taskRepository;
        private readonly IGenericRepository<User> _userRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IDistributedCache _cache;
        private readonly IOutboxRepository _outboxRepository;
        private readonly INotificationService _notificationService;

        public DeleteTaskCommandHandler(ITaskRepository taskRepository, IGenericRepository<User> userRepository, IUnitOfWork unitOfWork, IDistributedCache cache, IOutboxRepository outboxRepository, INotificationService notificationService)
        {
            _taskRepository = taskRepository;
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
            _cache = cache;
            _outboxRepository = outboxRepository;
            _notificationService = notificationService;
        }

        public async Task Handle(DeleteTaskCommand request, CancellationToken cancellationToken)
        {
            var task = await _taskRepository.GetByIdAsync(request.Id);
            if (task != null)
            {
                int userId = task.AssignedUserId;
                int categoryId = (int)task.Category;
                
                _taskRepository.Delete(task);

                var user = await _userRepository.GetByIdAsync(task.AssignedUserId);
                var userName = user != null ? $"{user.FirstName} {user.LastName}" : "Bilinmeyen Kullanıcı";
                await _notificationService.CreateNotificationsAsync($"{userName} kullanıcısına ait '{task.Title}' adlı görev silindi.", task.AssignedUserId, null);
                var outboxMessage = new OutboxMessage
                {
                    Type = "ReceiveTaskUpdate",
                    Payload = JsonSerializer.Serialize(new { Action = "Delete", TaskId = request.Id })
                };
                await _outboxRepository.AddAsync(outboxMessage);
            // Notification injection handled dynamically

                await _unitOfWork.SaveChangesAsync();
                
                await _cache.RemoveAsync("TaskStats_User_0_Cat_0");
                await _cache.RemoveAsync($"TaskStats_User_{userId}_Cat_0");
                await _cache.RemoveAsync($"TaskStats_User_0_Cat_{categoryId}");
                await _cache.RemoveAsync($"TaskStats_User_{userId}_Cat_{categoryId}");
            }
        }
    }

    // Update Task Status
    public class UpdateTaskStatusCommand : IRequest
    {
        public int Id { get; set; }
        public WorkStatus NewStatus { get; set; }
    }

    public class UpdateTaskStatusCommandHandler : IRequestHandler<UpdateTaskStatusCommand>
    {
        private readonly ITaskRepository _taskRepository;
        private readonly IGenericRepository<User> _userRepository;
        private readonly ITaskHistoryRepository _historyRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IDistributedCache _cache;
        private readonly IOutboxRepository _outboxRepository;
        private readonly INotificationService _notificationService;
        private readonly IMapper _mapper;

        public UpdateTaskStatusCommandHandler(ITaskRepository taskRepository, IGenericRepository<User> userRepository, ITaskHistoryRepository historyRepository, 
            IUnitOfWork unitOfWork, IDistributedCache cache, IOutboxRepository outboxRepository, INotificationService notificationService, IMapper mapper)
        {
            _taskRepository = taskRepository;
            _userRepository = userRepository;
            _historyRepository = historyRepository;
            _unitOfWork = unitOfWork;
            _cache = cache;
            _outboxRepository = outboxRepository;
            _notificationService = notificationService;
            _mapper = mapper;
        }

        public async Task Handle(UpdateTaskStatusCommand request, CancellationToken cancellationToken)
        {
            var task = await _taskRepository.GetByIdAsync(request.Id);
            if (task == null) throw new NotFoundException("Görev bulunamadı.");

            task.Status = request.NewStatus;
            _taskRepository.Update(task);

            var history = new TaskHistory 
            { 
                TaskId = task.Id, 
                ActionMessage = $"Görev durumu güncellendi: {request.NewStatus}" 
            };
            await _historyRepository.AddAsync(history);
            
            var user = await _userRepository.GetByIdAsync(task.AssignedUserId);
            var userName = user != null ? $"{user.FirstName} {user.LastName}" : "Bilinmeyen Kullanıcı";
            await _notificationService.CreateNotificationsAsync($"{userName} kullanıcısının '{task.Title}' görevi '{request.NewStatus}' durumuna geçti.", task.AssignedUserId, task.Id);
            var outboxMessage = new OutboxMessage
            {
                Type = "ReceiveTaskUpdate",
                Payload = JsonSerializer.Serialize(new { Action = "Update", Task = _mapper.Map<TaskResponseDto>(task) })
            };
            await _outboxRepository.AddAsync(outboxMessage);
            // Notification injection handled dynamically

            await _unitOfWork.SaveChangesAsync();

            await _cache.RemoveAsync("TaskStats_User_0_Cat_0");
            await _cache.RemoveAsync($"TaskStats_User_{task.AssignedUserId}_Cat_0");
            await _cache.RemoveAsync($"TaskStats_User_0_Cat_{(int)task.Category}");
            await _cache.RemoveAsync($"TaskStats_User_{task.AssignedUserId}_Cat_{(int)task.Category}");
        }
    }

    // Add Comment
    public class AddCommentCommand : IRequest
    {
        public int TaskId { get; set; }
        public int UserId { get; set; }
        public string Text { get; set; } = string.Empty;
    }

    public class AddCommentCommandHandler : IRequestHandler<AddCommentCommand>
    {
        private readonly ITaskCommentRepository _commentRepository;
        private readonly ITaskHistoryRepository _historyRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IOutboxRepository _outboxRepository;
        private readonly INotificationService _notificationService;

        public AddCommentCommandHandler(ITaskCommentRepository commentRepository, ITaskHistoryRepository historyRepository, IUnitOfWork unitOfWork, IOutboxRepository outboxRepository, INotificationService notificationService)
        {
            _commentRepository = commentRepository;
            _historyRepository = historyRepository;
            _unitOfWork = unitOfWork;
            _outboxRepository = outboxRepository;
            _notificationService = notificationService;
        }

        public async Task Handle(AddCommentCommand request, CancellationToken cancellationToken)
        {
            var comment = new TaskComment
            {
                TaskId = request.TaskId,
                UserId = request.UserId,
                Text = request.Text,
                CreatedDate = DateTime.UtcNow
            };
            await _commentRepository.AddAsync(comment);

            var history = new TaskHistory 
            { 
                TaskId = request.TaskId, 
                ActionMessage = "Göreve yeni bir not eklendi." 
            };
            await _historyRepository.AddAsync(history);

            await _notificationService.CreateNotificationsAsync($"#{request.TaskId} numaralı göreve yeni bir yorum yapıldı.", null, request.TaskId);
            var outboxMessage = new OutboxMessage
            {
                Type = "ReceiveNewComment",
                Payload = JsonSerializer.Serialize(request.TaskId)
            };
            await _outboxRepository.AddAsync(outboxMessage);
            // Notification injection handled dynamically

            await _unitOfWork.SaveChangesAsync();
        }
    }
}
