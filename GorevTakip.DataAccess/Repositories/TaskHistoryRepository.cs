using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GorevTakip.Entities;
using Microsoft.EntityFrameworkCore;

namespace GorevTakip.DataAccess.Repositories
{
    public class TaskHistoryRepository : GenericRepository<TaskHistory>, ITaskHistoryRepository
    {
        public TaskHistoryRepository(AppDbContext context) : base(context) 
        {
        }

        public async Task<IEnumerable<TaskHistory>> GetHistoryByTaskIdAsync(int taskId)
        {
            return await _context.TaskHistories
                .Where(h => h.TaskId == taskId) // İlgili görevin ID'sine göre filtrele
                .OrderByDescending(h => h.CreatedDate) // En son yapılan işlem en üstte (ilk sırada) çıksın diye Descending
                .ToListAsync();
        }
    }
}