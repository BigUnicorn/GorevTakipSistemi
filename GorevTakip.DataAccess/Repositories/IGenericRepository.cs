using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GorevTakip.DataAccess.Repositories
{
    public interface IGenericRepository<T> where T : class
    {
        // Önceki metotların (GetAllAsync, GetByIdAsync vs.) burada durabilir.
        Task<IEnumerable<T>> GetAllAsync();
        Task<T?> GetByIdAsync(int id);
        Task AddAsync(T entity);
        void Update(T entity);
        void Delete(T entity);
        
        System.Linq.IQueryable<T> GetQueryable();

        Task<T?> FirstOrDefaultAsync(System.Linq.Expressions.Expression<System.Func<T, bool>> predicate);
        Task<bool> AnyAsync(System.Linq.Expressions.Expression<System.Func<T, bool>> predicate);
    }
}