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

        public async Task<(IEnumerable<TaskItem> Tasks, int TotalRecords)> GetFilteredTasksWithUsersAsync(TaskFilterDto filter)
        {
            var query = _context.Tasks.Include(t => t.AssignedUser).AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.SearchText))
            {
                var search = filter.SearchText.ToLower();
                query = query.Where(x => x.Title.ToLower().Contains(search) || x.Description.ToLower().Contains(search));
            }

            if (filter.Status.HasValue && filter.Status.Value > 0)
                query = query.Where(x => (int)x.Status == filter.Status.Value);

            if (filter.AssignedUserId.HasValue && filter.AssignedUserId.Value > 0)
                query = query.Where(x => x.AssignedUserId == filter.AssignedUserId.Value);

            var totalRecords = await query.CountAsync();

            if (!string.IsNullOrEmpty(filter.SortBy))
            {
                switch (filter.SortBy.ToLower())
                {
                    case "title":
                        query = filter.SortDescending ? query.OrderByDescending(x => x.Title) : query.OrderBy(x => x.Title);
                        break;
                    case "description":
                        query = filter.SortDescending ? query.OrderByDescending(x => x.Description) : query.OrderBy(x => x.Description);
                        break;
                    case "duedate":
                        query = filter.SortDescending ? query.OrderByDescending(x => x.DueDate) : query.OrderBy(x => x.DueDate);
                        break;
                    case "assigneduser":
                        query = filter.SortDescending
                             ? query.OrderByDescending(x => x.AssignedUser!.FirstName).ThenByDescending(x => x.AssignedUser!.LastName)
                             : query.OrderBy(x => x.AssignedUser!.FirstName).ThenBy(x => x.AssignedUser!.LastName);
                        break;
                    case "status":
                        query = filter.SortDescending ? query.OrderByDescending(x => x.Status) : query.OrderBy(x => x.Status);
                        break;
                    default:
                        query = query.OrderByDescending(x => x.DueDate);
                        break;
                }
            }
            else
            {
                query = query.OrderByDescending(x => x.DueDate);
            }

            var tasks = await query
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return (tasks, totalRecords);
        }
    }
}