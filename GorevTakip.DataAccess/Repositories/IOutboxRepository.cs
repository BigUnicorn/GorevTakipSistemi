using GorevTakip.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GorevTakip.DataAccess.Repositories
{
    public interface IOutboxRepository : IGenericRepository<OutboxMessage>
    {
        Task<List<OutboxMessage>> GetUnprocessedMessagesAsync(int batchSize = 50);
    }
}
