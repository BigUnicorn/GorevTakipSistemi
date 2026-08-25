using GorevTakip.Entities;

namespace GorevTakip.Entities.DTOs
{
    public record UserRoleUpdateDto
    {
        public int UserId { get; set; }
        public UserRole NewRole { get; set; }
    }
}
