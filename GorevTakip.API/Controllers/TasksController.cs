using System;
using System.Security.Claims; // Token içindeki rol ve ID'yi okumak için EKLENDİ
using System.Threading.Tasks;
using GorevTakip.Business.Services;
using GorevTakip.Entities.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GorevTakip.API.Controllers
{
    [Authorize] // Frontend token gönderdiği için yetkilendirme zorunlu (Genel kural)
    [Route("api/[controller]")]
    [ApiController]
    public class TasksController : ControllerBase
    {
        private readonly ITaskService _taskService;

        public TasksController(ITaskService taskService)
        {
            _taskService = taskService;
        }

        // GET: api/Tasks
        [HttpGet]
        public async Task<IActionResult> GetAllTasks([FromQuery] TaskFilterDto filter)
        {
            try
            {
                // YENİ EKLENDİ: Token'dan kullanıcının rolünü okuyoruz
                var role = User.FindFirst(ClaimTypes.Role)?.Value;

                // YENİ EKLENDİ: Eğer kullanıcı Admin değilse, ZORUNLU olarak sadece kendi görevlerini listele
                if (role != "Admin") 
                {
                    var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (int.TryParse(userIdStr, out int userId))
                    {
                        filter.AssignedUserId = userId; // Frontend'den ne gelirse gelsin, kendi ID'sini eziyoruz
                    }
                }

                // İş katmanındaki GetFilteredTasksAsync metodunu çağırıyoruz
                var result = await _taskService.GetFilteredTasksAsync(filter);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Sunucu hatası: {ex.Message}");
            }
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
        [Authorize(Roles = "Admin")] // YENİ EKLENDİ: Sadece Admin rolüne sahip olanlar yeni görev ekleyebilir
        public async Task<IActionResult> CreateTask([FromBody] TaskCreateDto taskDto)
        {
            try
            {
                await _taskService.CreateTaskAsync(taskDto);
                return Ok("Görev başarıyla oluşturuldu.");
            }
            catch (Exception ex)
            {
                return BadRequest($"Görev eklenirken hata oluştu: {ex.Message}");
            }
        }

        // PUT: api/Tasks/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTask(int id, [FromBody] TaskUpdateDto taskDto)
        {
            if (id != taskDto.Id) 
                return BadRequest("URL içindeki ID ile gönderilen görev ID'si uyuşmuyor.");

            try
            {
                await _taskService.UpdateTaskAsync(taskDto);
                return Ok("Görev başarıyla güncellendi.");
            }
            catch (Exception ex)
            {
                return BadRequest($"Görev güncellenirken hata oluştu: {ex.Message}");
            }
        }

        // DELETE: api/Tasks/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")] // YENİ EKLENDİ: Sadece Admin rolüne sahip olanlar görev silebilir
        public async Task<IActionResult> DeleteTask(int id)
        {
            try
            {
                await _taskService.DeleteTaskAsync(id);
                return Ok("Görev başarıyla silindi.");
            }
            catch (Exception ex)
            {
                return BadRequest($"Görev silinirken hata oluştu: {ex.Message}");
            }
        }

        // GET: api/Tasks/statistics
        [HttpGet("statistics")]
        public async Task<IActionResult> GetTaskStatistics()
        {
            try
            {
                var role = User.FindFirst(ClaimTypes.Role)?.Value;
                int? userId = null;

                // Kullanıcı Admin değilse, sadece kendi istatistiklerini görebilsin
                if (role != "Admin")
                {
                    var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (int.TryParse(userIdStr, out int parsedUserId))
                    {
                        userId = parsedUserId;
                    }
                }

                var stats = await _taskService.GetTaskStatisticsAsync(userId);
                return Ok(stats);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Sunucu hatası: {ex.Message}");
            }
        }
    }
}