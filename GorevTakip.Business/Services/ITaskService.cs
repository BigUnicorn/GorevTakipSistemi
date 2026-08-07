using System.Collections.Generic;
using System.Threading.Tasks;
using GorevTakip.Entities.DTOs;

namespace GorevTakip.Business.Services
{
    public interface ITaskService
    {
        // Eski GetAllTasksAsync yerine bunu kullanacağız:
        Task<PagedResponseDto<TaskResponseDto>> GetFilteredTasksAsync(TaskFilterDto filter);

        // Senin var olan diğer metotların:
        Task<TaskResponseDto?> GetTaskByIdAsync(int id);
        Task<TaskResponseDto> CreateTaskAsync(TaskCreateDto taskCreateDto);
        Task UpdateTaskAsync(TaskUpdateDto taskUpdateDto);
        Task DeleteTaskAsync(int id);
        Task<TaskStatisticsDto> GetTaskStatisticsAsync(int? userId = null, int? categoryId = null);
        Task<IEnumerable<TaskHistoryDto>> GetTaskHistoryAsync(int taskId);
    }
}