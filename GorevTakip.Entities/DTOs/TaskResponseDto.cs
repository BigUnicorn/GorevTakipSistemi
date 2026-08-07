using System;
using GorevTakip.Entities;

namespace GorevTakip.Entities.DTOs
{
    public class TaskResponseDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public WorkStatus Status { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? DueDate { get; set; }
        public int AssignedUserId { get; set; }
        public string AssignedUserName { get; set; } = string.Empty;
        public TaskCategory Category { get; set; }
        public bool IsOverdue { get; set; }
    }
}