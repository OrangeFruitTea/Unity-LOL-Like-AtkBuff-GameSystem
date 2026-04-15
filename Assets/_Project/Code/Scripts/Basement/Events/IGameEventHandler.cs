using System;

namespace Basement.Events
{
    /// <summary>
    /// 事件处理器接口
    /// 定义事件处理器的标准行为
    /// </summary>
    /// <typeparam name="T">事件类型</typeparam>
    public interface IGameEventHandler<T> where T : IGameEvent
    {
        /// <summary>
        /// 处理事件
        /// </summary>
        /// <param name="eventData">事件数据</param>
        void Handle(T eventData);

        /// <summary>
        /// 处理器优先级
        /// </summary>
        GameEventPriority Priority { get; }
    }

    /// <summary>
    /// 事件处理器委托
    /// </summary>
    /// <typeparam name="T">事件类型</typeparam>
    /// <param name="eventData">事件数据</param>
    public delegate void GameEventHandlerDelegate<T>(T eventData) where T : IGameEvent;

    /// <summary>
    /// 事件订阅选项
    /// </summary>
    public class GameEventSubscriptionOptions
    {
        /// <summary>
        /// 订阅优先级
        /// </summary>
        public GameEventPriority Priority { get; set; } = GameEventPriority.Normal;

        /// <summary>
        /// 是否异步处理
        /// </summary>
        public bool IsAsync { get; set; } = false;

        /// <summary>
        /// 事件过滤条件
        /// </summary>
        public Func<IGameEvent, bool> Filter { get; set; } = null;
    }
}
