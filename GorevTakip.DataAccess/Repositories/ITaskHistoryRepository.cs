using GorevTakip.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GorevTakip.DataAccess.Repositories
{
    public interface ITaskHistoryRepository : IGenericRepository<TaskHistory>
    {
        Task<IEnumerable<TaskHistory>> GetHistoryByTaskIdAsync(int taskId);
    }
}