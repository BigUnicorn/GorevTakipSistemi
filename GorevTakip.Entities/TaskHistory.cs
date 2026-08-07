using System;

namespace GorevTakip.Entities
{
    public class TaskHistory
    {
        public int Id { get; set; }
        public int TaskId { get; set; }
        public TaskItem? Task { get; set; }
        public string ActionMessage { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}