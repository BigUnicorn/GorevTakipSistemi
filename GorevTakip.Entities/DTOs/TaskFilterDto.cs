namespace GorevTakip.Entities.DTOs
{
    public class TaskFilterDto
    {
        public string? SearchText { get; set; }
        public int? Status { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int? AssignedUserId { get; set; }
    }
}