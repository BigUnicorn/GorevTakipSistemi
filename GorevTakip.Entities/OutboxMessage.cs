using System;

namespace GorevTakip.Entities
{
    public class OutboxMessage
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Type { get; set; } = string.Empty;
        public string Payload { get; set; } = string.Empty;
        public DateTime OccurredOnUtc { get; set; } = DateTime.UtcNow;
        public DateTime? ProcessedOnUtc { get; set; }
        public string? Error { get; set; }
    }
}
