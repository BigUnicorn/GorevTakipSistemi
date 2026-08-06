using System;

namespace GorevTakip.Entities.DTOs
{
    public class TaskCreateDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime? DueDate { get; set; }
        public int AssignedUserId { get; set; }
        public TaskCategory Category { get; set; }
    }
}