using System;
using System.Text.Json.Serialization; // Bu kütüphaneyi ekliyoruz

namespace GorevTakip.Entities
{
    public class TaskItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        
        public WorkStatus Status { get; set; } = WorkStatus.Todo;
        public TaskCategory Category { get; set; } = TaskCategory.Backend;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? DueDate { get; set; }

        public int AssignedUserId { get; set; }
        
        public User? AssignedUser { get; set; }
    }
}