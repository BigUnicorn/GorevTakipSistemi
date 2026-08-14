using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GorevTakip.Entities;
using Microsoft.EntityFrameworkCore;

namespace GorevTakip.DataAccess.Repositories
{
    // Hem GenericRepository'den temel işlemleri devralıyor hem de kendi Interface'ini uyguluyor
    public class TaskCommentRepository : GenericRepository<TaskComment>, ITaskCommentRepository
    {
        // AppDbContext'i alıp, miras aldığı base (GenericRepository) sınıfa iletiyor
        public TaskCommentRepository(AppDbContext context) : base(context) 
        {
        }

        public async Task<IEnumerable<TaskComment>> GetCommentsWithUserByTaskIdAsync(int taskId)
        {
            return await _context.Comments
                .Include(c => c.User) // SQL'deki INNER JOIN gibi düşünün, yorumu yapan kullanıcı bilgisini de getirir.
                .Where(c => c.TaskId == taskId) // Sadece istenen göreve ait yorumları filtreler.
                .OrderBy(c => c.CreatedDate) // Eskiden yeniye doğru sıralar.
                .ToListAsync(); // Sorguyu çalıştırıp listeye çevirir.
        }
    }
}