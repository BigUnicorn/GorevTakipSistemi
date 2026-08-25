using GorevTakip.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GorevTakip.DataAccess.Repositories
{
    public class OutboxRepository : GenericRepository<OutboxMessage>, IOutboxRepository
    {
        public OutboxRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<List<OutboxMessage>> GetUnprocessedMessagesAsync(int batchSize = 50)
        {
            return await _context.OutboxMessages
                .Where(m => m.ProcessedOnUtc == null)
                .OrderBy(m => m.OccurredOnUtc)
                .Take(batchSize)
                .ToListAsync();
        }
    }
}
