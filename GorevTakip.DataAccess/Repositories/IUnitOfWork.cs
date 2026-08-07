using System;
using System.Threading.Tasks;

namespace GorevTakip.DataAccess.Repositories
{
    public interface IUnitOfWork : IAsyncDisposable
    {
        Task<int> SaveChangesAsync();
    }
}