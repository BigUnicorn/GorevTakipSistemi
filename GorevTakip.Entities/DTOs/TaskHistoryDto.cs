using System;

namespace GorevTakip.Entities.DTOs
{
    public record TaskHistoryDto
    {
        public string ActionMessage { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
    }
}
