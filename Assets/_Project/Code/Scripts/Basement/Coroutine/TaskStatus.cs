namespace Basement.Threading
{
    /// <summary>
    /// 任务状态枚举
    /// </summary>
    public enum TaskStatus
    {
        /// <summary>
        /// 任务已创建
        /// </summary>
        Created = 0,

        /// <summary>
        /// 等待依赖任务完成
        /// </summary>
        Waiting = 1,

        /// <summary>
        /// 就绪，可以执行
        /// </summary>
        Ready = 2,

        /// <summary>
        /// 正在执行
        /// </summary>
        Running = 3,

        /// <summary>
        /// 执行完成
        /// </summary>
        Completed = 4,

        /// <summary>
        /// 执行失败
        /// </summary>
        Failed = 5,

        /// <summary>
        /// 已取消
        /// </summary>
        Canceled = 6
    }
}
