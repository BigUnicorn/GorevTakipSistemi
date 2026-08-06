namespace GorevTakip.Entities.DTOs
{
    public class TaskStatisticsDto
    {
        public int TotalTasks { get; set; }
        public int TodoTasks { get; set; }
        public int InProgressTasks { get; set; }
        public int CompletedTasks { get; set; }
    }
}