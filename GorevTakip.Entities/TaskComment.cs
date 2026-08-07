using System;

namespace GorevTakip.Entities
{
    public class TaskComment
    {
        public int Id { get; set; }
        public int TaskId { get; set; }
        public TaskItem? Task { get; set; }
        public int UserId { get; set; }
        public User? User { get; set; }
        public string Text { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}