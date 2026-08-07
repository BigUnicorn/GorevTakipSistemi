using System;

namespace GorevTakip.Entities.DTOs
{
    public class TaskCommentDto
    {
        public int Id { get; set; }
        public string Text { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
    }
}