using System.Collections.Generic;
using System.Text.Json.Serialization; // Bunu en üste eklemeyi unutma!

namespace GorevTakip.Entities
{
    public class User
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty; 
        
        public string? RefreshToken { get; set; }
        public System.DateTime? RefreshTokenExpiryTime { get; set; }

        public UserRole Role { get; set; } = UserRole.Employee;

        [JsonIgnore] // Bunu ekledik!
        public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
    }
}