using System;

namespace Basement.Events
{
    /// <summary>
    /// 事件接口
    /// 所有事件类型都需要实现此接口
    /// </summary>
    public interface IGameEvent
    {
        /// <summary>
        /// 事件唯一标识
        /// </summary>
        string EventId { get; }

        /// <summary>
        /// 事件时间戳
        /// </summary>
        DateTime Timestamp { get; set; }

        /// <summary>
        /// 事件优先级
        /// </summary>
        GameEventPriority Priority { get; }
    }

    /// <summary>
    /// 事件优先级枚举
    /// </summary>
    public enum GameEventPriority
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
