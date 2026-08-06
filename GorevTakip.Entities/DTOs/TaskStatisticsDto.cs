namespace GorevTakip.Entities.DTOs
{
    public class TaskStatisticsDto
    {
        public int TotalTasks { get; set; }
        public int TodoTasks { get; set; }
        public int InProgressTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int FrontendTasks { get; set; }
        public int BackendTasks { get; set; }
        public int DatabaseTasks { get; set; }
        public int BugFixTasks { get; set; }
        public int MobileTasks { get; set; }
        public int DevOpsTasks { get; set; }
    }
}