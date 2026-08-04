using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using GorevTakip.Business.Services;
using GorevTakip.Entities;
using GorevTakip.Entities.DTOs;
using System.Threading.Tasks;

namespace GorevTakip.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class TasksController : ControllerBase
    {
        private readonly ITaskService _taskService;

        public TasksController(ITaskService taskService)
        {
            _taskService = taskService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var tasks = await _taskService.GetAllTasksAsync();
            return Ok(tasks);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var task = await _taskService.GetTaskByIdAsync(id);
            if (task == null)
                return NotFound("Görev bulunamadı.");
            
            return Ok(task);
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetByUserId(int userId)
        {
            var tasks = await _taskService.GetTasksByUserIdAsync(userId);
            return Ok(tasks);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] TaskCreateDto taskDto)
        {
            try
            {
                var createdTask = await _taskService.CreateTaskAsync(taskDto);
                return CreatedAtAction(nameof(GetById), new { id = createdTask.Id }, createdTask);
            }
            catch (System.Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] TaskUpdateDto taskDto)
        {
            if (id != taskDto.Id)
                return BadRequest("URL'deki ID ile gövdedeki ID uyuşmuyor.");

            try
            {
                await _taskService.UpdateTaskAsync(taskDto);
                return NoContent();
            }
            catch (System.Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _taskService.DeleteTaskAsync(id);
            return NoContent();
        }
        
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> ChangeStatus(int id, [FromBody] WorkStatus newStatus)
        {
            try
            {
                await _taskService.UpdateTaskStatusAsync(id, newStatus);
                return NoContent();
            }
            catch (System.Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}