using GorevTakip.Entities;

namespace GorevTakip.Entities.DTOs
{
    public class UserRoleUpdateDto
    {
        public int UserId { get; set; }
        public UserRole NewRole { get; set; }
    }
}