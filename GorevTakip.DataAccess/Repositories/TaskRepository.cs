using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GorevTakip.Entities;
using GorevTakip.Entities.DTOs;
using Microsoft.EntityFrameworkCore;

namespace GorevTakip.DataAccess.Repositories
{
    public class TaskRepository : GenericRepository<TaskItem>, ITaskRepository
    {
        public TaskRepository(AppDbContext context) : base(context)
        {
        }
    }
}