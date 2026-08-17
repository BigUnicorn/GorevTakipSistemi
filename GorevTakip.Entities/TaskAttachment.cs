using System;
using System.Text.Json.Serialization;

namespace GorevTakip.Entities
{
    public class TaskAttachment
    {
        public int Id { get; set; }
        public int TaskId { get; set; }
        
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long FileSize { get; set; }
        
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
        public int UploadedByUserId { get; set; }

        [JsonIgnore]
        public TaskItem? Task { get; set; }
        
        [JsonIgnore]
        public User? UploadedByUser { get; set; }
    }
}
