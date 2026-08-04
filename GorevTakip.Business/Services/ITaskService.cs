using System.Collections.Generic;
using System.Threading.Tasks;
using GorevTakip.Entities;
using GorevTakip.Entities.DTOs; // DTO kütüphanesini ekledik

namespace GorevTakip.Business.Services
{
    public interface ITaskService
    {
        Task<IEnumerable<TaskResponseDto>> GetAllTasksAsync();
        Task<TaskResponseDto?> GetTaskByIdAsync(int id);
        Task<IEnumerable<TaskResponseDto>> GetTasksByUserIdAsync(int userId);
        Task<TaskResponseDto> CreateTaskAsync(TaskCreateDto taskDto);
        Task UpdateTaskAsync(TaskUpdateDto taskDto);
        Task DeleteTaskAsync(int id);
        Task UpdateTaskStatusAsync(int id, WorkStatus newStatus);
    }
}