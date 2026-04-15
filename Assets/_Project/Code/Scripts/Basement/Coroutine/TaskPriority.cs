namespace Basement.Threading
{
    /// <summary>
    /// 任务优先级枚举
    /// </summary>
    public enum TaskPriority
    {
        /// <summary>
        /// 低优先级
        /// </summary>
        Low = 0,

        /// <summary>
        /// 普通优先级
        /// </summary>
        Normal = 1,

        /// <summary>
        /// 高优先级
        /// </summary>
        High = 2,

        /// <summary>
        /// 紧急优先级
        /// </summary>
        Critical = 3
    }
}
