using GorevTakip.Entities;

namespace GorevTakip.DataAccess.Repositories
{
    public class TaskAttachmentRepository : GenericRepository<TaskAttachment>, ITaskAttachmentRepository
    {
        public TaskAttachmentRepository(AppDbContext context) : base(context)
        {
        }
    }
}
