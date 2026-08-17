using System;
using System.Security.Claims; // Token içindeki rol ve ID'yi okumak için EKLENDİ
using System.Threading.Tasks;
using GorevTakip.Business.Services;
using GorevTakip.Entities.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GorevTakip.Entities;
using Microsoft.AspNetCore.SignalR;
using GorevTakip.API.Hubs;

namespace GorevTakip.API.Controllers
{
    [Authorize] // Frontend token gönderdiği için yetkilendirme zorunlu (Genel kural)
    [Route("api/[controller]")]
    [ApiController]
    public class TasksController : ControllerBase
    {
        private readonly ITaskService _taskService;
        private readonly IHubContext<TaskHub> _hubContext;

        public TasksController(ITaskService taskService, IHubContext<TaskHub> hubContext)
        {
            _taskService = taskService;
            _hubContext = hubContext;
        }

        // GET: api/Tasks
        [HttpGet]
        public async Task<IActionResult> GetAllTasks([FromQuery] TaskFilterDto filter)
        {
            // Token'dan kullanıcının rolünü okuyoruz
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            // YENİ HALİ: "Admin" string'i yerine nameof(UserRole.Admin) kullanıyoruz
            if (role != nameof(UserRole.Admin)) 
            {
                var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(userIdStr, out int userId))
                {
                    filter.AssignedUserId = userId; // Frontend'den ne gelirse gelsin, kendi ID'sini eziyoruz
                }
                else
                {
                    return Unauthorized();
                }
            }

            // İş katmanındaki GetFilteredTasksAsync metodunu çağırıyoruz
            var result = await _taskService.GetFilteredTasksAsync(filter);
            return Ok(result);
        }

        // GET: api/Tasks/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetTaskById(int id)
        {
            var task = await _taskService.GetTaskByIdAsync(id);
            if (task == null) 
                return NotFound("Görev bulunamadı.");
                
            return Ok(task);
        }

        // POST: api/Tasks
        [HttpPost]
        // YENİ HALİ: Sadece Admin rolüne sahip olanlar yeni görev ekleyebilir
        [Authorize(Roles = nameof(UserRole.Admin))] 
        public async Task<IActionResult> CreateTask([FromBody] TaskCreateDto taskDto)
        {
            var createdTask = await _taskService.CreateTaskAsync(taskDto);
            await _hubContext.Clients.All.SendAsync(HubConstants.ReceiveTaskUpdate, new { Action = "Create", Task = createdTask });
            return Ok("Görev başarıyla oluşturuldu.");
        }

        // PUT: api/Tasks/5
        [HttpPut("{id}")]
        [Authorize(Roles = nameof(UserRole.Admin))]
        public async Task<IActionResult> UpdateTask(int id, [FromBody] TaskUpdateDto taskDto)
        {
            if (id != taskDto.Id) 
                return BadRequest("URL içindeki ID ile gönderilen görev ID'si uyuşmuyor.");

            await _taskService.UpdateTaskAsync(taskDto);
            var updatedTask = await _taskService.GetTaskByIdAsync(taskDto.Id);
            await _hubContext.Clients.All.SendAsync(HubConstants.ReceiveTaskUpdate, new { Action = "Update", Task = updatedTask });
            return Ok("Görev başarıyla güncellendi.");
        }

        // PATCH: api/Tasks/5/status
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateTaskStatus(int id, [FromBody] WorkStatus newStatus)
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (!int.TryParse(userIdStr, out int userId))
                return Unauthorized();

            // Eğer Admin değilse, sadece kendi görevini güncelleyebilir
            if (role != nameof(UserRole.Admin))
            {
                var task = await _taskService.GetTaskByIdAsync(id);
                if (task == null) return NotFound("Görev bulunamadı.");
                
                if (task.AssignedUserId != userId)
                    return Forbid("Sadece size atanan görevlerin durumunu güncelleyebilirsiniz."); 
            }

            await _taskService.UpdateTaskStatusAsync(id, newStatus);
            var updatedTask = await _taskService.GetTaskByIdAsync(id);
            await _hubContext.Clients.All.SendAsync(HubConstants.ReceiveTaskUpdate, new { Action = "Update", Task = updatedTask });
            return Ok("Görev durumu başarıyla güncellendi.");
        }

        // DELETE: api/Tasks/5
        [HttpDelete("{id}")]
        // YENİ HALİ: Sadece Admin rolüne sahip olanlar görev silebilir
        [Authorize(Roles = nameof(UserRole.Admin))] 
        public async Task<IActionResult> DeleteTask(int id)
        {
            await _taskService.DeleteTaskAsync(id);
            await _hubContext.Clients.All.SendAsync(HubConstants.ReceiveTaskUpdate, new { Action = "Delete", TaskId = id });
            return Ok("Görev başarıyla silindi.");
        }

        // GET: api/Tasks/statistics?userId=5&categoryId=2
        [HttpGet("statistics")]
        public async Task<IActionResult> GetTaskStatistics([FromQuery] int? userId, [FromQuery] int? categoryId)
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            // YENİ HALİ: Eğer kullanıcı Admin değilse, dışarıdan ne gönderilirse gönderilsin kendi ID'sini eziyoruz (Güvenlik)
            if (role != nameof(UserRole.Admin))
            {
                var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(userIdStr, out int parsedUserId))
                {
                    userId = parsedUserId;
                }
                else
                {
                    return Unauthorized();
                }
            }
            
            // Admin ise ve parametre olarak bir userId gönderdiyse o kullanıcınınkini, 
            // göndermediyse (veya 0/null ise) tüm sistemin istatistiğini getirir.
            var stats = await _taskService.GetTaskStatisticsAsync(userId, categoryId);
            return Ok(stats);
        }

        // GET: api/Tasks/5/history
        [HttpGet("{id}/history")]
        public async Task<IActionResult> GetTaskHistory(int id)
        {
            var history = await _taskService.GetTaskHistoryAsync(id);
            return Ok(history);
        }

        [HttpGet("{id}/comments")]
        public async Task<IActionResult> GetComments(int id)
        {
            var comments = await _taskService.GetCommentsAsync(id);
            return Ok(comments);
        }

        [HttpPost("{id}/comments")]
        public async Task<IActionResult> AddComment(int id, [FromBody] TaskCommentCreateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Text)) return BadRequest("Yorum boş olamaz.");
            
            // Yorumu yapan kişiyi tokenden güvenli bir şekilde alıyoruz
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdStr, out int userId)) return Unauthorized();

            await _taskService.AddCommentAsync(id, userId, dto.Text);
            await _hubContext.Clients.All.SendAsync(HubConstants.ReceiveNewComment, id);
            return Ok("Yorum eklendi.");
        }
    }
}