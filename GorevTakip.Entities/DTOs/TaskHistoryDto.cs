using System;

namespace GorevTakip.Entities.DTOs
{
    public class TaskHistoryDto
    {
        public string ActionMessage { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
    }
}