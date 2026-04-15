namespace Basement.Threading
{
    /// <summary>
    /// 任务统计信息
    /// </summary>
    public class TaskStatistics
    {
        public int TotalCount { get; set; }
        public int CreatedCount { get; set; }
        public int WaitingCount { get; set; }
        public int ReadyCount { get; set; }
        public int RunningCount { get; set; }
        public int CompletedCount { get; set; }
        public int FailedCount { get; set; }
        public int CanceledCount { get; set; }
    }
}
