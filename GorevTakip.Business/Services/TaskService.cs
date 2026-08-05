using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GorevTakip.DataAccess.Repositories;
using GorevTakip.Entities;
using GorevTakip.Entities.DTOs;
using Microsoft.EntityFrameworkCore;

namespace GorevTakip.Business.Services
{
    public class TaskService : ITaskService
    {
        private readonly IGenericRepository<TaskItem> _taskRepository;
        private readonly IGenericRepository<User> _userRepository;

        public TaskService(IGenericRepository<TaskItem> taskRepository, IGenericRepository<User> userRepository)
        {
            _taskRepository = taskRepository;
            _userRepository = userRepository;
        }

        // --- YENİ EKLENEN FİLTRELEME VE SAYFALAMA METODU ---
        public async Task<PagedResponseDto<TaskResponseDto>> GetFilteredTasksAsync(TaskFilterDto filter)
        {
            // 1. Sorguyu Başlat (Henüz SQL'e gitmedi)
            var query = _taskRepository.GetQueryable();

            // 2. Metin Araması (SearchText)
            if (!string.IsNullOrWhiteSpace(filter.SearchText))
            {
                var search = filter.SearchText.ToLower();
                // Veritabanı tarafında büyük/küçük harf duyarlılığını kaldırmak için ToLower() kullanıyoruz
                query = query.Where(x => x.Title.ToLower().Contains(search) || 
                                         x.Description.ToLower().Contains(search));
            }

            // 3. Durum Filtresi (Status)
            if (filter.Status.HasValue && filter.Status.Value > 0)
            {
                // Enum yapısına göre cast (int) işlemi gerekebilir
                query = query.Where(x => (int)x.Status == filter.Status.Value);
            }

            // Toplam kayıt sayısını al (Sayfalama hesabı için - EF Core CountAsync gerektirir)
            var totalRecords = await query.CountAsync();

            // 4. Sayfalama (Pagination) İşlemi
            var tasks = await query
                .OrderByDescending(x => x.DueDate) // Yaklaşan görevleri/son eklenenleri üste alıyoruz
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync(); // SQL sorgusu veritabanında burada çalışır!

            // 5. Entity'den DTO'ya dönüştürme
            var mappedTasks = tasks.Select(t => new TaskResponseDto
                {
                    Id = t.Id,
                    Title = t.Title,
                    Description = t.Description,
                    
                    // HATA BURADAYDI! 
                    // filter.Status.Value yazan yeri silip aşağıdaki gibi değiştirmelisin:
                    Status = t.Status, 
                    
                    DueDate = t.DueDate,
                    AssignedUserId = t.AssignedUserId
                }).ToList();
            // 6. Yanıtı Döndür
            return new PagedResponseDto<TaskResponseDto>
            {
                Data = mappedTasks,
                TotalRecords = totalRecords,
                TotalPages = (int)Math.Ceiling((double)totalRecords / filter.PageSize),
                CurrentPage = filter.PageNumber
            };
        }

        public async Task<IEnumerable<TaskResponseDto>> GetAllTasksAsync()
        {
            var tasks = await _taskRepository.GetAllAsync();
            return tasks.Select(MapToResponseDto);
        }

        public async Task<TaskResponseDto?> GetTaskByIdAsync(int id)
        {
            var task = await _taskRepository.GetByIdAsync(id);
            if (task == null) return null;
            return MapToResponseDto(task);
        }

        public async Task<IEnumerable<TaskResponseDto>> GetTasksByUserIdAsync(int userId)
        {
            var tasks = await _taskRepository.GetAllAsync();
            return tasks.Where(t => t.AssignedUserId == userId).Select(MapToResponseDto);
        }

        public async Task<TaskResponseDto> CreateTaskAsync(TaskCreateDto taskDto)
        {
            var userExists = await _userRepository.GetByIdAsync(taskDto.AssignedUserId);
            if (userExists == null)
                throw new Exception("Atanan kullanıcı bulunamadı!");

            // Dışarıdan gelen DTO'yu, veritabanına kaydedilecek Entity'ye dönüştürüyoruz
            var taskItem = new TaskItem
            {
                Title = taskDto.Title,
                Description = taskDto.Description,
                DueDate = taskDto.DueDate,
                AssignedUserId = taskDto.AssignedUserId,
                Status = WorkStatus.Todo,
                CreatedDate = DateTime.UtcNow
            };

            await _taskRepository.AddAsync(taskItem);
            await _taskRepository.SaveChangesAsync();

            return MapToResponseDto(taskItem);
        }

        public async Task UpdateTaskAsync(TaskUpdateDto taskDto)
        {
            var existingTask = await _taskRepository.GetByIdAsync(taskDto.Id);
            if (existingTask == null)
                throw new Exception("Güncellenecek görev bulunamadı.");

            var userExists = await _userRepository.GetByIdAsync(taskDto.AssignedUserId);
            if (userExists == null)
                throw new Exception("Atanan kullanıcı bulunamadı!");

            existingTask.Title = taskDto.Title;
            existingTask.Description = taskDto.Description;
            existingTask.Status = taskDto.Status;
            existingTask.DueDate = taskDto.DueDate;
            existingTask.AssignedUserId = taskDto.AssignedUserId;

            _taskRepository.Update(existingTask);
            await _taskRepository.SaveChangesAsync();
        }

        public async Task DeleteTaskAsync(int id)
        {
            var task = await _taskRepository.GetByIdAsync(id);
            if (task != null)
            {
                _taskRepository.Delete(task);
                await _taskRepository.SaveChangesAsync();
            }
        }

        public async Task UpdateTaskStatusAsync(int id, WorkStatus newStatus)
        {
            var task = await _taskRepository.GetByIdAsync(id);
            if (task == null)
                throw new Exception("Görev bulunamadı.");

            task.Status = newStatus;
            _taskRepository.Update(task);
            await _taskRepository.SaveChangesAsync();
        }

        // YARDIMCI METOT: Veritabanından gelen Entity'i dışarıya verilecek DTO'ya dönüştürür
        private TaskResponseDto MapToResponseDto(TaskItem task)
        {
            return new TaskResponseDto
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Description,
                Status = task.Status,
                CreatedDate = task.CreatedDate,
                DueDate = task.DueDate,
                AssignedUserId = task.AssignedUserId
            };
        }
    }
}