using GorevTakip.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GorevTakip.DataAccess.Repositories
{
    // IGenericRepository<TaskComment>'i miras alarak temel CRUD (Ekle/Sil vb.) operasyonlarını hazır alıyoruz.
    public interface ITaskCommentRepository : IGenericRepository<TaskComment>
    {
        // Kendimize has, sadece yorumlara özel metodumuz:
        Task<IEnumerable<TaskComment>> GetCommentsWithUserByTaskIdAsync(int taskId);
    }
}