using System;
using System.Security.Claims;
using Asp.Versioning;
using System.Threading.Tasks;
using GorevTakip.Business.Features.Tasks.Commands;
using GorevTakip.Business.Features.Tasks.Queries;
using GorevTakip.Entities.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GorevTakip.Entities;
using MediatR;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace GorevTakip.API.Controllers
{
    [Authorize] // Frontend token gönderdiği için yetkilendirme zorunlu (Genel kural)
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    public class TasksController : ControllerBase
    {
        private readonly IMediator _mediator;

        public TasksController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // GET: api/Tasks
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PagedResponseDto<TaskResponseDto>))]
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

            var result = await _mediator.Send(new GetFilteredTasksQuery { Filter = filter });
            return Ok(result);
        }

        // GET: api/Tasks/5
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TaskResponseDto))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetTaskById(int id)
        {
            var task = await _mediator.Send(new GetTaskByIdQuery { Id = id });
            if (task == null) 
                return NotFound("Görev bulunamadı.");
                
            return Ok(task);
        }

        // POST: api/Tasks
        [HttpPost]
        // YENİ HALİ: Sadece Admin rolüne sahip olanlar yeni görev ekleyebilir
        [Authorize(Roles = nameof(UserRole.Admin))] 
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(string))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateTask([FromBody] TaskCreateDto taskDto)
        {
            await _mediator.Send(new CreateTaskCommand { TaskDto = taskDto });
            return Ok("Görev başarıyla oluşturuldu.");
        }

        // PUT: api/Tasks/5
        [HttpPut("{id}")]
        [Authorize(Roles = nameof(UserRole.Admin))]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(string))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateTask(int id, [FromBody] TaskUpdateDto taskDto)
        {
            if (id != taskDto.Id) 
                return BadRequest("URL içindeki ID ile gönderilen görev ID'si uyuşmuyor.");

            await _mediator.Send(new UpdateTaskCommand { TaskDto = taskDto });
            return Ok("Görev başarıyla güncellendi.");
        }

        // PATCH: api/Tasks/5/status
        [HttpPatch("{id}/status")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(string))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateTaskStatus(int id, [FromBody] WorkStatus newStatus)
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (!int.TryParse(userIdStr, out int userId))
                return Unauthorized();

            // Eğer Admin değilse, sadece kendi görevini güncelleyebilir
            if (role != nameof(UserRole.Admin))
            {
                var task = await _mediator.Send(new GetTaskByIdQuery { Id = id });
                if (task == null) return NotFound("Görev bulunamadı.");
                
                if (task.AssignedUserId != userId)
                    return Forbid("Sadece size atanan görevlerin durumunu güncelleyebilirsiniz."); 
            }

            await _mediator.Send(new UpdateTaskStatusCommand { Id = id, NewStatus = newStatus });
            return Ok("Görev durumu başarıyla güncellendi.");
        }

        // DELETE: api/Tasks/5
        [HttpDelete("{id}")]
        // YENİ HALİ: Sadece Admin rolüne sahip olanlar görev silebilir
        [Authorize(Roles = nameof(UserRole.Admin))] 
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(string))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteTask(int id)
        {
            await _mediator.Send(new DeleteTaskCommand { Id = id });
            return Ok("Görev başarıyla silindi.");
        }

        // GET: api/Tasks/statistics?userId=5&categoryId=2
        [HttpGet("statistics")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TaskStatisticsDto))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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
            var stats = await _mediator.Send(new GetTaskStatisticsQuery { UserId = userId, CategoryId = categoryId });
            return Ok(stats);
        }

        // GET: api/Tasks/5/history
        [HttpGet("{id}/history")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<TaskHistoryDto>))]
        public async Task<IActionResult> GetTaskHistory(int id)
        {
            var history = await _mediator.Send(new GetTaskHistoryQuery { TaskId = id });
            return Ok(history);
        }

        [HttpGet("{id}/comments")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<TaskCommentDto>))]
        public async Task<IActionResult> GetComments(int id)
        {
            var comments = await _mediator.Send(new GetCommentsQuery { TaskId = id });
            return Ok(comments);
        }

        [HttpPost("{id}/comments")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(string))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> AddComment(int id, [FromBody] TaskCommentCreateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Text)) return BadRequest("Yorum boş olamaz.");
            
            // Yorumu yapan kişiyi tokenden güvenli bir şekilde alıyoruz
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdStr, out int userId)) return Unauthorized();

            await _mediator.Send(new AddCommentCommand { TaskId = id, UserId = userId, Text = dto.Text });
            return Ok("Yorum eklendi.");
        }
    }
}