using System;

namespace GorevTakip.Entities
{
    public class Notification
    {
        public int Id { get; set; }
        
        // Kime ait olduğu
        public int UserId { get; set; }
        public User? User { get; set; }

        public string Message { get; set; } = string.Empty;
        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Hangi görev ile ilgili (Tıklandığında göreve gidebilmek için)
        public int? RelatedTaskId { get; set; }
    }
}
