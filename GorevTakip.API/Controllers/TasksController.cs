using System;
using System.Threading.Tasks;
using GorevTakip.Business.Services;
using GorevTakip.Entities.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GorevTakip.API.Controllers
{
    [Authorize] // Frontend token gönderdiği için yetkilendirme zorunlu
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
        // 4. Maddede eklediğimiz, sayfalamalı ve filtreli yeni listeleme metodu
        [HttpGet]
        public async Task<IActionResult> GetAllTasks([FromQuery] TaskFilterDto filter)
        {
            try
            {
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
    }
}