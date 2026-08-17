using GorevTakip.Entities;
using GorevTakip.Entities.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GorevTakip.DataAccess.Repositories
{
    public interface ITaskRepository : IGenericRepository<TaskItem>
    {
        Task<(IEnumerable<TaskItem> Tasks, int TotalRecords)> GetFilteredTasksWithUsersAsync(TaskFilterDto filter);
    }
}