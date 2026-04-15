using System;
using System.Collections.Generic;
using System.Linq;
using Basement.Utils;

namespace Basement.Events
{
    /// <summary>
    /// 事件总线
    /// 负责事件的注册、订阅和分发
    /// </summary>
    public class GameEventBus : Singleton<GameEventBus>
    {
        private readonly Dictionary<Type, List<GameEventHandlerInfo>> _eventHandlers = new Dictionary<Type, List<GameEventHandlerInfo>>();
        private readonly object _lock = new object();
        private bool _isInitialized = false;

        public void Initialize()
        {
            if (_isInitialized) return;

            _isInitialized = true;
        }

        /// <summary>
        /// 订阅事件
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="handler">事件处理器</param>
        public void Subscribe<T>(GameEventHandlerDelegate<T> handler) where T : IGameEvent
        {
            Subscribe(handler, new GameEventSubscriptionOptions());
        }

        /// <summary>
        /// 订阅事件（带选项）
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="handler">事件处理器</param>
        /// <param name="options">订阅选项</param>
        public void Subscribe<T>(GameEventHandlerDelegate<T> handler, GameEventSubscriptionOptions options) where T : IGameEvent
        {
            if (handler == null)
            {
                return;
            }

            Type eventType = typeof(T);

            lock (_lock)
            {
                if (!_eventHandlers.ContainsKey(eventType))
                {
                    _eventHandlers[eventType] = new List<GameEventHandlerInfo>();
                }

                var handlerInfo = new GameEventHandlerInfo
                {
                    Handler = handler,
                    Priority = options.Priority,
                    IsAsync = options.IsAsync,
                    Filter = options.Filter
                };

                _eventHandlers[eventType].Add(handlerInfo);
                // 按优先级排序
                _eventHandlers[eventType].Sort((a, b) => b.Priority.CompareTo(a.Priority));
            }
        }

        /// <summary>
        /// 取消订阅事件
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="handler">事件处理器</param>
        public void Unsubscribe<T>(GameEventHandlerDelegate<T> handler) where T : IGameEvent
        {
            if (handler == null) return;

            Type eventType = typeof(T);

            lock (_lock)
            {
                if (_eventHandlers.ContainsKey(eventType))
                {
                    _eventHandlers[eventType].RemoveAll(h => h.Handler == handler);
                }
            }
        }

        /// <summary>
        /// 发布事件
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="eventData">事件数据</param>
        public void Publish<T>(T eventData) where T : IGameEvent
        {
            if (eventData == null)
            {
                return;
            }

            Type eventType = typeof(T);

            // 设置事件时间戳
            if (eventData.Timestamp == default)
            {
                eventData.Timestamp = DateTime.Now;
            }

            List<GameEventHandlerInfo> handlers;

            lock (_lock)
            {
                if (!_eventHandlers.TryGetValue(eventType, out handlers))
                {
                    return;
                }
                // 创建副本以避免并发修改
                handlers = handlers.ToList();
            }

            foreach (var handlerInfo in handlers)
            {
                // 应用静态过滤条件
                if (handlerInfo.Filter != null && !handlerInfo.Filter(eventData))
                {
                    continue;
                }

                if (handlerInfo.IsAsync)
                {
                    // 异步处理
                    System.Threading.ThreadPool.QueueUserWorkItem(state =>
                    {
                        try
                        {
                            handlerInfo.Handler.DynamicInvoke(eventData);
                        }
                        catch (Exception ex)
                        {
                            // 处理异常
                        }
                    });
                }
                else
                {
                    // 同步处理
                    try
                    {
                        handlerInfo.Handler.DynamicInvoke(eventData);
                    }
                    catch (Exception ex)
                    {
                        // 处理异常
                    }
                }
            }
        }

        /// <summary>
        /// 异步发布事件
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="eventData">事件数据</param>
        public void PublishAsync<T>(T eventData) where T : IGameEvent
        {
            if (eventData == null)
            {
                return;
            }

            // 设置事件时间戳
            if (eventData.Timestamp == default)
            {
                eventData.Timestamp = DateTime.Now;
            }

            // 在后台线程处理事件
            System.Threading.ThreadPool.QueueUserWorkItem(state =>
            {
                Publish(eventData);
            });
        }

        /// <summary>
        /// 清空所有订阅
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                _eventHandlers.Clear();
            }
        }

        /// <summary>
        /// 清空指定事件的订阅
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        public void Clear<T>() where T : IGameEvent
        {
            Type eventType = typeof(T);

            lock (_lock)
            {
                if (_eventHandlers.ContainsKey(eventType))
                {
                    _eventHandlers[eventType].Clear();
                }
            }
        }

        /// <summary>
        /// 获取事件处理器数量
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <returns>处理器数量</returns>
        public int GetHandlerCount<T>() where T : IGameEvent
        {
            Type eventType = typeof(T);

            lock (_lock)
            {
                if (_eventHandlers.TryGetValue(eventType, out var handlers))
                {
                    return handlers.Count;
                }
                return 0;
            }
        }

        private class GameEventHandlerInfo
        {
            public Delegate Handler { get; set; }
            public GameEventPriority Priority { get; set; }
            public bool IsAsync { get; set; }
            public Func<IGameEvent, bool> Filter { get; set; }
        }
    }
}
