using System;

namespace Basement.Tasks
{
    /// <summary>
    /// 任务接口
    /// 定义任务的标准行为
    /// </summary>
    public interface ITimingTask
    {
        /// <summary>
        /// 任务ID
        /// </summary>
        string TaskId { get; }

        /// <summary>
        /// 任务状态
        /// </summary>
        TimingTaskState State { get; }

        /// <summary>
        /// 任务优先级
        /// </summary>
        TimingTaskPriority Priority { get; }

        /// <summary>
        /// 任务延迟时间（秒）
        /// </summary>
        float DelayTime { get; }

        /// <summary>
        /// 执行任务
        /// </summary>
        void Execute();

        /// <summary>
        /// 取消任务
        /// </summary>
        void Cancel();
    }

    /// <summary>
    /// 任务状态枚举
    /// </summary>
    public enum TimingTaskState
    {
        /// <summary>
        /// 就绪
        /// </summary>
        Ready,

        /// <summary>
        /// 执行中
        /// </summary>
        Running,

        /// <summary>
        /// 完成
        /// </summary>
        Completed
    }

    /// <summary>
    /// 任务优先级枚举
    /// </summary>
    public enum TimingTaskPriority
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
        High = 2
    }
}
