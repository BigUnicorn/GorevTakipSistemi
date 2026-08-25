namespace GorevTakip.Entities.DTOs
{
    public record UserLoginDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
