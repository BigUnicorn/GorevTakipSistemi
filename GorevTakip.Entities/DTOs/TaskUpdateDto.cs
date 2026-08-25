using System;

namespace GorevTakip.Entities.DTOs
{
    public record TaskUpdateDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public WorkStatus Status { get; set; }
        public DateTime? DueDate { get; set; }
        public int AssignedUserId { get; set; }
        public TaskCategory Category { get; set; }
    }
}
