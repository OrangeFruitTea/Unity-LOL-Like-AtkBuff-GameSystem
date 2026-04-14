# 事件调度服务技术文档

## 概述

事件调度服务是核心业务层的核心组件，负责游戏内事件的分发、订阅和处理。该服务采用观察者模式实现，支持同步和异步事件处理、事件优先级、事件过滤、事件历史记录等功能，为游戏系统提供高效、灵活的事件通信机制。

## 模块架构设计

### 1. 设计目标

- **解耦通信**：实现模块间的松耦合通信，降低系统复杂度
- **高性能**：优化事件分发性能，支持大量事件的实时处理
- **灵活性**：支持多种事件类型、优先级、过滤条件等
- **可靠性**：确保事件不丢失，支持事件重试和错误处理
- **易于扩展**：支持自定义事件类型和处理器

### 2. 架构分层

```
├── 事件调度服务
│   ├── 核心接口
│   │   ├── IEvent (事件接口)
│   │   │   ├── EventId
│   │   │   ├── Timestamp
│   │   │   ├── Priority
│   │   │   └── IsHandled
│   │   └── IEventHandler<T> (事件处理器接口)
│   │       ├── Handle(eventData)
│   │       ├── Priority
│   │       └── IsAsync
│   ├── 核心组件
│   │   ├── EventBus (事件总线)
│   │   │   ├── 订阅事件 (Subscribe)
│   │   │   ├── 取消订阅 (Unsubscribe)
│   │   │   ├── 发布事件 (Publish)
│   │   │   ├── 异步发布 (PublishAsync)
│   │   │   └── 清空订阅 (Clear)
│   │   ├── EventScheduler (事件调度器)
│   │   │   ├── 调度事件 (Schedule)
│   │   │   ├── 延迟调度 (ScheduleDelayed)
│   │   │   ├── 处理队列 (ProcessQueue)
│   │   │   ├── 更新 (Update)
│   │   │   └── 清空队列 (ClearQueue)
│   │   ├── EventFilterManager (事件过滤器)
│   │   │   ├── 添加过滤器 (AddFilter)
│   │   │   ├── 移除过滤器 (RemoveFilter)
│   │   │   ├── 过滤事件 (Filter) 
│   │   │   └── 清空过滤器 (Clear)
│   │   └── EventHistoryRecorder (事件历史记录)
│   │       ├── 记录事件 (RecordEvent)
│   │       ├── 获取记录 (GetRecords)
│   │       ├── 按事件ID获取记录 (GetRecordsByEventId)
│   │       ├── 导出记录 (ExportRecords)
│   │       └── 清空记录 (Clear)
│   ├── 辅助组件
│   │   ├── PriorityQueue<T> (优先级队列)
│   │   ├── EventPool<T> (事件池化)
│   │   ├── BatchEventProcessor (批量事件处理)
│   │   └── EventDeduplicator (事件去重)
│   └── 与Unity集成
│       ├── MonoBehaviour集成
│       ├── 编辑器工具
│       └── 协程支持
```

### 3. 核心组件

#### 3.1 事件接口

```csharp
namespace Basement.Events
{
    /// <summary>
    /// 事件接口
    /// 所有事件类型都需要实现此接口
    /// </summary>
    public interface IEvent
    {
        /// <summary>
        /// 事件唯一标识
        /// </summary>
        string EventId { get; }

        /// <summary>
        /// 事件时间戳
        /// </summary>
        DateTime Timestamp { get; }

        /// <summary>
        /// 事件优先级
        /// </summary>
        EventPriority Priority { get; }

        /// <summary>
        /// 事件是否已处理
        /// </summary>
        bool IsHandled { get; set; }
    }

    /// <summary>
    /// 事件优先级枚举
    /// </summary>
    public enum EventPriority
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
```

#### 3.2 事件处理器接口

```csharp
namespace Basement.Events
{
    /// <summary>
    /// 事件处理器接口
    /// 定义事件处理器的标准行为
    /// </summary>
    /// <typeparam name="T">事件类型</typeparam>
    public interface IEventHandler<T> where T : IEvent
    {
        /// <summary>
        /// 处理事件
        /// </summary>
        /// <param name="eventData">事件数据</param>
        void Handle(T eventData);

        /// <summary>
        /// 处理器优先级
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// 是否异步处理
        /// </summary>
        bool IsAsync { get; }
    }

    /// <summary>
    /// 事件处理器委托
    /// </summary>
    /// <typeparam name="T">事件类型</typeparam>
    /// <param name="eventData">事件数据</param>
    public delegate void EventHandlerDelegate<T>(T eventData) where T : IEvent;
}
```

#### 3.3 事件总线

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using Basement.Logging;
using Basement.Utils;

namespace Basement.Events
{
    /// <summary>
    /// 事件总线
    /// 负责事件的注册、订阅和分发
    /// </summary>
    public class EventBus : Singleton<EventBus>
    {
        private readonly Dictionary<Type, List<Delegate>> _eventHandlers = new Dictionary<Type, List<Delegate>>();
        private readonly Dictionary<Type, List<HandlerInfo>> _handlerInfos = new Dictionary<Type, List<HandlerInfo>>();
        private readonly object _lock = new object();
        private bool _isInitialized = false;

        public void Initialize()
        {
            if (_isInitialized) return;

            _isInitialized = true;
            LogManager.Instance.LogInfo("事件总线初始化完成", "EventBus");
        }

        /// <summary>
        /// 订阅事件
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="handler">事件处理器</param>
        public void Subscribe<T>(EventHandlerDelegate<T> handler) where T : IEvent
        {
            if (handler == null)
            {
                LogManager.Instance.LogWarning("事件处理器不能为空", "EventBus");
                return;
            }

            Type eventType = typeof(T);

            lock (_lock)
            {
                if (!_eventHandlers.ContainsKey(eventType))
                {
                    _eventHandlers[eventType] = new List<Delegate>();
                }

                if (!_eventHandlers[eventType].Contains(handler))
                {
                    _eventHandlers[eventType].Add(handler);
                    LogManager.Instance.LogDebug($"订阅事件: {eventType.Name}", "EventBus");
                }
            }
        }

        /// <summary>
        /// 订阅事件（带优先级）
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="handler">事件处理器</param>
        /// <param name="priority">优先级</param>
        public void Subscribe<T>(EventHandlerDelegate<T> handler, int priority) where T : IEvent
        {
            if (handler == null)
            {
                LogManager.Instance.LogWarning("事件处理器不能为空", "EventBus");
                return;
            }

            Type eventType = typeof(T);

            lock (_lock)
            {
                if (!_handlerInfos.ContainsKey(eventType))
                {
                    _handlerInfos[eventType] = new List<HandlerInfo>();
                }

                HandlerInfo handlerInfo = new HandlerInfo
                {
                    Handler = handler,
                    Priority = priority
                };

                _handlerInfos[eventType].Add(handlerInfo);
                _handlerInfos[eventType].Sort((a, b) => b.Priority.CompareTo(a.Priority));

                LogManager.Instance.LogDebug($"订阅事件 [优先级: {priority}]: {eventType.Name}", "EventBus");
            }
        }

        /// <summary>
        /// 取消订阅事件
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="handler">事件处理器</param>
        public void Unsubscribe<T>(EventHandlerDelegate<T> handler) where T : IEvent
        {
            if (handler == null) return;

            Type eventType = typeof(T);

            lock (_lock)
            {
                if (_eventHandlers.ContainsKey(eventType))
                {
                    _eventHandlers[eventType].Remove(handler);
                    LogManager.Instance.LogDebug($"取消订阅事件: {eventType.Name}", "EventBus");
                }

                if (_handlerInfos.ContainsKey(eventType))
                {
                    _handlerInfos[eventType].RemoveAll(h => h.Handler == handler);
                }
            }
        }

        /// <summary>
        /// 发布事件
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="eventData">事件数据</param>
        public void Publish<T>(T eventData) where T : IEvent
        {
            if (eventData == null)
            {
                LogManager.Instance.LogWarning("事件数据不能为空", "EventBus");
                return;
            }

            Type eventType = typeof(T);

            lock (_lock)
            {
                // 设置事件时间戳
                if (eventData.Timestamp == default)
                {
                    eventData.Timestamp = DateTime.Now;
                }

                // 处理无优先级的处理器
                if (_eventHandlers.ContainsKey(eventType))
                {
                    var handlers = _eventHandlers[eventType].ToList();

                    foreach (var handler in handlers)
                    {
                        try
                        {
                            handler.DynamicInvoke(eventData);
                        }
                        catch (Exception ex)
                        {
                            LogManager.Instance.LogError($"事件处理失败 [{eventType.Name}]: {ex.Message}", "EventBus");
                        }
                    }
                }

                // 处理带优先级的处理器
                if (_handlerInfos.ContainsKey(eventType))
                {
                    var handlerInfos = _handlerInfos[eventType].ToList();

                    foreach (var handlerInfo in handlerInfos)
                    {
                        try
                        {
                            handlerInfo.Handler.DynamicInvoke(eventData);
                        }
                        catch (Exception ex)
                        {
                            LogManager.Instance.LogError($"事件处理失败 [{eventType.Name}]: {ex.Message}", "EventBus");
                        }
                    }
                }

                LogManager.Instance.LogDebug($"发布事件: {eventType.Name}", "EventBus");
            }
        }

        /// <summary>
        /// 异步发布事件
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="eventData">事件数据</param>
        public void PublishAsync<T>(T eventData) where T : IEvent
        {
            if (eventData == null)
            {
                LogManager.Instance.LogWarning("事件数据不能为空", "EventBus");
                return;
            }

            Type eventType = typeof(T);

            lock (_lock)
            {
                if (eventData.Timestamp == default)
                {
                    eventData.Timestamp = DateTime.Now;
                }
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
                _handlerInfos.Clear();
                LogManager.Instance.LogInfo("清空所有事件订阅", "EventBus");
            }
        }

        /// <summary>
        /// 清空指定事件的订阅
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        public void Clear<T>() where T : IEvent
        {
            Type eventType = typeof(T);

            lock (_lock)
            {
                if (_eventHandlers.ContainsKey(eventType))
                {
                    _eventHandlers[eventType].Clear();
                }

                if (_handlerInfos.ContainsKey(eventType))
                {
                    _handlerInfos[eventType].Clear();
                }

                LogManager.Instance.LogInfo($"清空事件订阅: {eventType.Name}", "EventBus");
            }
        }

        /// <summary>
        /// 获取事件处理器数量
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <returns>处理器数量</returns>
        public int GetHandlerCount<T>() where T : IEvent
        {
            Type eventType = typeof(T);

            lock (_lock)
            {
                int count = 0;

                if (_eventHandlers.ContainsKey(eventType))
                {
                    count += _eventHandlers[eventType].Count;
                }

                if (_handlerInfos.ContainsKey(eventType))
                {
                    count += _handlerInfos[eventType].Count;
                }

                return count;
            }
        }

        private class HandlerInfo
        {
            public Delegate Handler { get; set; }
            public int Priority { get; set; }
        }
    }
}
```

#### 3.4 事件调度器

```csharp
using System;
using System.Collections.Generic;
using Basement.Logging;
using Basement.Utils;

namespace Basement.Events
{
    /// <summary>
    /// 事件调度器
    /// 负责事件的队列管理和调度
    /// </summary>
    public class EventScheduler : Singleton<EventScheduler>
    {
        private readonly PriorityQueue<IEvent> _eventQueue = new PriorityQueue<IEvent>();
        private readonly List<IEvent> _processingEvents = new List<IEvent>();
        private readonly object _lock = new object();
        private bool _isProcessing = false;
        private int _maxConcurrentEvents = 10;
        private float _processInterval = 0.016f; // 约60FPS
        private float _lastProcessTime = 0f;

        public void Initialize()
        {
            LogManager.Instance.LogInfo("事件调度器初始化完成", "EventScheduler");
        }

        /// <summary>
        /// 调度事件
        /// </summary>
        /// <param name="eventData">事件数据</param>
        public void Schedule(IEvent eventData)
        {
            if (eventData == null)
            {
                LogManager.Instance.LogWarning("事件数据不能为空", "EventScheduler");
                return;
            }

            lock (_lock)
            {
                _eventQueue.Enqueue(eventData, (int)eventData.Priority);
                LogManager.Instance.LogDebug($"调度事件: {eventData.EventId} [优先级: {eventData.Priority}]", "EventScheduler");
            }
        }

        /// <summary>
        /// 延迟调度事件
        /// </summary>
        /// <param name="eventData">事件数据</param>
        /// <param name="delay">延迟时间（秒）</param>
        public void ScheduleDelayed(IEvent eventData, float delay)
        {
            if (eventData == null)
            {
                LogManager.Instance.LogWarning("事件数据不能为空", "EventScheduler");
                return;
            }

            TimerManager.Instance.Schedule(delay, () =>
            {
                Schedule(eventData);
            });

            LogManager.Instance.LogDebug($"延迟调度事件: {eventData.EventId} [延迟: {delay}秒]", "EventScheduler");
        }

        /// <summary>
        /// 处理事件队列
        /// </summary>
        public void ProcessQueue()
        {
            if (_isProcessing) return;

            lock (_lock)
            {
                if (_eventQueue.Count == 0) return;

                _isProcessing = true;
            }

            try
            {
                int processedCount = 0;

                while (processedCount < _maxConcurrentEvents)
                {
                    IEvent eventData;

                    lock (_lock)
                    {
                        if (_eventQueue.Count == 0) break;

                        eventData = _eventQueue.Dequeue();
                        _processingEvents.Add(eventData);
                    }

                    try
                    {
                        EventBus.Instance.Publish(eventData);
                        processedCount++;
                    }
                    catch (Exception ex)
                    {
                        LogManager.Instance.LogError($"处理事件失败 [{eventData.EventId}]: {ex.Message}", "EventScheduler");
                    }
                    finally
                    {
                        lock (_lock)
                        {
                            _processingEvents.Remove(eventData);
                        }
                    }
                }

                if (processedCount > 0)
                {
                    LogManager.Instance.LogDebug($"处理事件: {processedCount}个", "EventScheduler");
                }
            }
            finally
            {
                _isProcessing = false;
            }
        }

        /// <summary>
        /// 更新调度器
        /// </summary>
        public void Update()
        {
            if (UnityEngine.Time.time - _lastProcessTime >= _processInterval)
            {
                ProcessQueue();
                _lastProcessTime = UnityEngine.Time.time;
            }
        }

        /// <summary>
        /// 清空事件队列
        /// </summary>
        public void ClearQueue()
        {
            lock (_lock)
            {
                _eventQueue.Clear();
                _processingEvents.Clear();
                LogManager.Instance.LogInfo("清空事件队列", "EventScheduler");
            }
        }

        /// <summary>
        /// 获取队列大小
        /// </summary>
        public int QueueSize
        {
            get
            {
                lock (_lock)
                {
                    return _eventQueue.Count;
                }
            }
        }

        /// <summary>
        /// 设置最大并发事件数
        /// </summary>
        public void SetMaxConcurrentEvents(int max)
        {
            _maxConcurrentEvents = Math.Max(1, max);
            LogManager.Instance.LogInfo($"最大并发事件数设置为: {_maxConcurrentEvents}", "EventScheduler");
        }

        /// <summary>
        /// 设置处理间隔
        /// </summary>
        public void SetProcessInterval(float interval)
        {
            _processInterval = Math.Max(0.001f, interval);
            LogManager.Instance.LogInfo($"处理间隔设置为: {_processInterval}秒", "EventScheduler");
        }
    }

    /// <summary>
    /// 优先级队列
    /// </summary>
    /// <typeparam name="T">元素类型</typeparam>
    public class PriorityQueue<T>
    {
        private readonly List<KeyValuePair<int, T>> _elements = new List<KeyValuePair<int, T>>();

        public int Count => _elements.Count;

        public void Enqueue(T item, int priority)
        {
            _elements.Add(new KeyValuePair<int, T>(priority, item));
            _elements.Sort((a, b) => b.Key.CompareTo(a.Key));
        }

        public T Dequeue()
        {
            if (_elements.Count == 0)
            {
                throw new InvalidOperationException("队列为空");
            }

            T item = _elements[0].Value;
            _elements.RemoveAt(0);
            return item;
        }

        public void Clear()
        {
            _elements.Clear();
        }
    }
}
```

#### 3.5 事件过滤器

```csharp
using System;
using System.Collections.Generic;
using Basement.Logging;

namespace Basement.Events
{
    /// <summary>
    /// 事件过滤器接口
    /// </summary>
    public interface IEventFilter
    {
        /// <summary>
        /// 是否通过过滤器
        /// </summary>
        /// <param name="eventData">事件数据</param>
        /// <returns>是否通过</returns>
        bool Pass(IEvent eventData);
    }

    /// <summary>
    /// 事件过滤器管理器
    /// </summary>
    public class EventFilterManager
    {
        private readonly List<IEventFilter> _filters = new List<IEventFilter>();
        private readonly object _lock = new object();

        /// <summary>
        /// 添加过滤器
        /// </summary>
        /// <param name="filter">过滤器</param>
        public void AddFilter(IEventFilter filter)
        {
            if (filter == null) return;

            lock (_lock)
            {
                if (!_filters.Contains(filter))
                {
                    _filters.Add(filter);
                    LogManager.Instance.LogDebug($"添加事件过滤器: {filter.GetType().Name}", "EventFilterManager");
                }
            }
        }

        /// <summary>
        /// 移除过滤器
        /// </summary>
        /// <param name="filter">过滤器</param>
        public void RemoveFilter(IEventFilter filter)
        {
            if (filter == null) return;

            lock (_lock)
            {
                _filters.Remove(filter);
                LogManager.Instance.LogDebug($"移除事件过滤器: {filter.GetType().Name}", "EventFilterManager");
            }
        }

        /// <summary>
        /// 过滤事件
        /// </summary>
        /// <param name="eventData">事件数据</param>
        /// <returns>是否通过所有过滤器</returns>
        public bool Filter(IEvent eventData)
        {
            if (eventData == null) return false;

            lock (_lock)
            {
                foreach (var filter in _filters)
                {
                    if (!filter.Pass(eventData))
                    {
                        LogManager.Instance.LogDebug($"事件被过滤器拒绝: {eventData.EventId}", "EventFilterManager");
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// 清空所有过滤器
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                _filters.Clear();
                LogManager.Instance.LogInfo("清空所有事件过滤器", "EventFilterManager");
            }
        }
    }

    /// <summary>
    /// 事件ID过滤器
    /// </summary>
    public class EventIdFilter : IEventFilter
    {
        private readonly HashSet<string> _allowedEventIds;

        public EventIdFilter(params string[] eventIds)
        {
            _allowedEventIds = new HashSet<string>(eventIds);
        }

        public bool Pass(IEvent eventData)
        {
            return eventData != null && _allowedEventIds.Contains(eventData.EventId);
        }
    }

    /// <summary>
    /// 事件优先级过滤器
    /// </summary>
    public class EventPriorityFilter : IEventFilter
    {
        private readonly EventPriority _minPriority;

        public EventPriorityFilter(EventPriority minPriority)
        {
            _minPriority = minPriority;
        }

        public bool Pass(IEvent eventData)
        {
            return eventData != null && eventData.Priority >= _minPriority;
        }
    }

    /// <summary>
    /// 事件类型过滤器
    /// </summary>
    public class EventTypeFilter : IEventFilter
    {
        private readonly Type _eventType;

        public EventTypeFilter(Type eventType)
        {
            _eventType = eventType;
        }

        public bool Pass(IEvent eventData)
        {
            return eventData != null && eventData.GetType() == _eventType;
        }
    }
}
```

#### 3.6 事件历史记录

```csharp
using System;
using System.Collections.Generic;
using Basement.Logging;
using Basement.Utils;

namespace Basement.Events
{
    /// <summary>
    /// 事件历史记录器
    /// 负责记录事件的历史信息
    /// </summary>
    public class EventHistoryRecorder : Singleton<EventHistoryRecorder>
    {
        private readonly Queue<EventRecord> _eventRecords = new Queue<EventRecord>();
        private readonly object _lock = new object();
        private int _maxRecords = 1000;
        private bool _isRecording = false;

        public void Initialize(int maxRecords = 1000)
        {
            _maxRecords = maxRecords;
            _isRecording = true;
            LogManager.Instance.LogInfo($"事件历史记录器初始化完成 [最大记录数: {_maxRecords}]", "EventHistoryRecorder");
        }

        /// <summary>
        /// 记录事件
        /// </summary>
        /// <param name="eventData">事件数据</param>
        public void RecordEvent(IEvent eventData)
        {
            if (!_isRecording || eventData == null) return;

            lock (_lock)
            {
                EventRecord record = new EventRecord
                {
                    EventId = eventData.EventId,
                    EventType = eventData.GetType().Name,
                    Timestamp = eventData.Timestamp,
                    Priority = eventData.Priority,
                    IsHandled = eventData.IsHandled
                };

                _eventRecords.Enqueue(record);

                // 超过最大记录数时移除旧记录
                while (_eventRecords.Count > _maxRecords)
                {
                    _eventRecords.Dequeue();
                }
            }
        }

        /// <summary>
        /// 获取事件记录
        /// </summary>
        /// <param name="count">记录数量</param>
        /// <returns>事件记录列表</returns>
        public List<EventRecord> GetRecords(int count = 10)
        {
            lock (_lock)
            {
                List<EventRecord> records = new List<EventRecord>();

                foreach (var record in _eventRecords)
                {
                    records.Add(record);
                    if (records.Count >= count) break;
                }

                return records;
            }
        }

        /// <summary>
        /// 根据事件ID获取记录
        /// </summary>
        /// <param name="eventId">事件ID</param>
        /// <returns>事件记录列表</returns>
        public List<EventRecord> GetRecordsByEventId(string eventId)
        {
            lock (_lock)
            {
                List<EventRecord> records = new List<EventRecord>();

                foreach (var record in _eventRecords)
                {
                    if (record.EventId == eventId)
                    {
                        records.Add(record);
                    }
                }

                return records;
            }
        }

        /// <summary>
        /// 清空记录
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                _eventRecords.Clear();
                LogManager.Instance.LogInfo("清空事件历史记录", "EventHistoryRecorder");
            }
        }

        /// <summary>
        /// 导出记录
        /// </summary>
        /// <param name="filePath">文件路径</param>
        public void ExportRecords(string filePath)
        {
            lock (_lock)
            {
                try
                {
                    using (var writer = new System.IO.StreamWriter(filePath))
                    {
                        writer.WriteLine("EventId,EventType,Timestamp,Priority,IsHandled");

                        foreach (var record in _eventRecords)
                        {
                            writer.WriteLine($"{record.EventId},{record.EventType},{record.Timestamp},{record.Priority},{record.IsHandled}");
                        }
                    }

                    LogManager.Instance.LogInfo($"导出事件记录到: {filePath}", "EventHistoryRecorder");
                }
                catch (Exception ex)
                {
                    LogManager.Instance.LogError($"导出事件记录失败: {ex.Message}", "EventHistoryRecorder");
                }
            }
        }

        /// <summary>
        /// 开始记录
        /// </summary>
        public void StartRecording()
        {
            _isRecording = true;
            LogManager.Instance.LogInfo("开始记录事件历史", "EventHistoryRecorder");
        }

        /// <summary>
        /// 停止记录
        /// </summary>
        public void StopRecording()
        {
            _isRecording = false;
            LogManager.Instance.LogInfo("停止记录事件历史", "EventHistoryRecorder");
        }
    }

    /// <summary>
    /// 事件记录
    /// </summary>
    public class EventRecord
    {
        public string EventId { get; set; }
        public string EventType { get; set; }
        public DateTime Timestamp { get; set; }
        public EventPriority Priority { get; set; }
        public bool IsHandled { get; set; }
    }
}
```

## 使用说明

### 1. 定义事件

```csharp
using Basement.Events;

[System.Serializable]
public class PlayerDamageEvent : IEvent
{
    public string EventId => "PlayerDamage";
    public DateTime Timestamp { get; set; }
    public EventPriority Priority { get; set; }
    public bool IsHandled { get; set; }

    public int PlayerId;
    public int Damage;
    public int RemainingHealth;
    public string DamageSource;

    public PlayerDamageEvent(int playerId, int damage, int remainingHealth, string damageSource)
    {
        PlayerId = playerId;
        Damage = damage;
        RemainingHealth = remainingHealth;
        DamageSource = damageSource;
        Priority = EventPriority.Normal;
        IsHandled = false;
    }
}
```

### 2. 订阅和处理事件

```csharp
using UnityEngine;
using Basement.Events;

public class PlayerHealthSystem : MonoBehaviour
{
    private void Start()
    {
        // 初始化事件总线
        EventBus.Instance.Initialize();

        // 订阅事件
        EventBus.Instance.Subscribe<PlayerDamageEvent>(OnPlayerDamage);

        // 订阅事件（带优先级）
        EventBus.Instance.Subscribe<PlayerDamageEvent>(OnPlayerDamageHighPriority, 100);
    }

    private void OnDestroy()
    {
        // 取消订阅
        EventBus.Instance.Unsubscribe<PlayerDamageEvent>(OnPlayerDamage);
        EventBus.Instance.Unsubscribe<PlayerDamageEvent>(OnPlayerDamageHighPriority);
    }

    private void OnPlayerDamage(PlayerDamageEvent eventData)
    {
        Debug.Log($"玩家 {eventData.PlayerId} 受到 {eventData.Damage} 点伤害，剩余生命值: {eventData.RemainingHealth}");

        // 更新UI显示
        UpdateHealthUI(eventData.PlayerId, eventData.RemainingHealth);
    }

    private void OnPlayerDamageHighPriority(PlayerDamageEvent eventData)
    {
        Debug.Log($"高优先级处理: 玩家 {eventData.PlayerId} 受到 {eventData.Damage} 点伤害");

        // 检查是否死亡
        if (eventData.RemainingHealth <= 0)
        {
            HandlePlayerDeath(eventData.PlayerId);
        }
    }

    private void UpdateHealthUI(int playerId, int health)
    {
        // 更新UI逻辑
    }

    private void HandlePlayerDeath(int playerId)
    {
        // 处理死亡逻辑
    }
}
```

### 3. 发布事件

```csharp
using UnityEngine;
using Basement.Events;

public class CombatSystem : MonoBehaviour
{
    public void DealDamage(int attackerId, int targetId, int damage)
    {
        // 计算伤害
        int actualDamage = CalculateDamage(damage);
        int remainingHealth = GetPlayerHealth(targetId) - actualDamage;

        // 发布事件
        PlayerDamageEvent damageEvent = new PlayerDamageEvent(
            targetId,
            actualDamage,
            remainingHealth,
            $"Player_{attackerId}"
        );

        EventBus.Instance.Publish(damageEvent);

        // 异步发布事件
        EventBus.Instance.PublishAsync(damageEvent);
    }

    private int CalculateDamage(int baseDamage)
    {
        return baseDamage;
    }

    private int GetPlayerHealth(int playerId)
    {
        return 100;
    }
}
```

### 4. 使用事件调度器

```csharp
using UnityEngine;
using Basement.Events;

public class EventSchedulerExample : MonoBehaviour
{
    private void Start()
    {
        // 初始化事件调度器
        EventScheduler.Instance.Initialize();

        // 订阅事件
        EventBus.Instance.Subscribe<DelayedEvent>(OnDelayedEvent);
    }

    private void Update()
    {
        // 更新事件调度器
        EventScheduler.Instance.Update();
    }

    public void ScheduleDelayedEvent()
    {
        // 创建延迟事件
        DelayedEvent delayedEvent = new DelayedEvent
        {
            Message = "延迟事件触发"
        };

        // 延迟调度事件（3秒后触发）
        EventScheduler.Instance.ScheduleDelayed(delayedEvent, 3f);
    }

    private void OnDelayedEvent(DelayedEvent eventData)
    {
        Debug.Log(eventData.Message);
    }
}

[System.Serializable]
public class DelayedEvent : IEvent
{
    public string EventId => "DelayedEvent";
    public DateTime Timestamp { get; set; }
    public EventPriority Priority { get; set; }
    public bool IsHandled { get; set; }

    public string Message;
}
```

### 5. 使用事件过滤器

```csharp
using UnityEngine;
using Basement.Events;

public class EventFilterExample : MonoBehaviour
{
    private EventFilterManager _filterManager;

    private void Start()
    {
        _filterManager = new EventFilterManager();

        // 添加事件ID过滤器
        _filterManager.AddFilter(new EventIdFilter("PlayerDamage", "PlayerDeath"));

        // 添加优先级过滤器
        _filterManager.AddFilter(new EventPriorityFilter(EventPriority.High));

        // 添加类型过滤器
        _filterManager.AddFilter(new EventTypeFilter(typeof(PlayerDamageEvent)));
    }

    public void PublishFilteredEvent(IEvent eventData)
    {
        // 检查是否通过过滤器
        if (_filterManager.Filter(eventData))
        {
            EventBus.Instance.Publish(eventData);
        }
        else
        {
            Debug.Log($"事件被过滤器拒绝: {eventData.EventId}");
        }
    }
}
```

### 6. 使用事件历史记录

```csharp
using UnityEngine;
using Basement.Events;

public class EventHistoryExample : MonoBehaviour
{
    private void Start()
    {
        // 初始化事件历史记录器
        EventHistoryRecorder.Instance.Initialize(500);

        // 订阅事件并记录
        EventBus.Instance.Subscribe<PlayerDamageEvent>(OnPlayerDamageWithRecord);
    }

    private void OnPlayerDamageWithRecord(PlayerDamageEvent eventData)
    {
        // 记录事件
        EventHistoryRecorder.Instance.RecordEvent(eventData);

        // 处理事件
        Debug.Log($"处理事件: {eventData.EventId}");
    }

    public void ShowRecentEvents()
    {
        // 获取最近10条记录
        var records = EventHistoryRecorder.Instance.GetRecords(10);

        Debug.Log("最近的事件记录:");
        foreach (var record in records)
        {
            Debug.Log($"[{record.Timestamp}] {record.EventId} - {record.EventType}");
        }
    }

    public void ExportEventHistory()
    {
        // 导出事件记录
        string filePath = System.IO.Path.Combine(UnityEngine.Application.persistentDataPath, "EventHistory.csv");
        EventHistoryRecorder.Instance.ExportRecords(filePath);
    }
}
```

## 性能优化策略

### 1. 事件池化

```csharp
public class EventPool<T> where T : IEvent, new()
{
    private readonly Stack<T> _pool = new Stack<T>();
    private readonly object _lock = new object();

    public T Get()
    {
        lock (_lock)
        {
            if (_pool.Count > 0)
            {
                return _pool.Pop();
            }
            return new T();
        }
    }

    public void Return(T eventData)
    {
        if (eventData == null) return;

        lock (_lock)
        {
            _pool.Push(eventData);
        }
    }
}
```

### 2. 批量事件处理

```csharp
public class BatchEventProcessor
{
    private readonly List<IEvent> _batch = new List<IEvent>();
    private readonly int _batchSize = 100;
    private readonly float _batchInterval = 0.1f;
    private float _lastBatchTime = 0f;

    public void AddEvent(IEvent eventData)
    {
        _batch.Add(eventData);

        if (_batch.Count >= _batchSize ||
            UnityEngine.Time.time - _lastBatchTime >= _batchInterval)
        {
            ProcessBatch();
        }
    }

    private void ProcessBatch()
    {
        if (_batch.Count == 0) return;

        foreach (var eventData in _batch)
        {
            EventBus.Instance.Publish(eventData);
        }

        _batch.Clear();
        _lastBatchTime = UnityEngine.Time.time;
    }
}
```

### 3. 事件去重

```csharp
public class EventDeduplicator
{
    private readonly HashSet<string> _recentEventIds = new HashSet<string>();
    private readonly Queue<string> _eventQueue = new Queue<string>();
    private readonly int _maxRecentEvents = 1000;

    public bool ShouldProcessEvent(IEvent eventData)
    {
        if (eventData == null) return false;

        string eventKey = $"{eventData.EventId}_{eventData.Timestamp.Ticks}";

        if (_recentEventIds.Contains(eventKey))
        {
            return false;
        }

        _recentEventIds.Add(eventKey);
        _eventQueue.Enqueue(eventKey);

        // 限制最近事件数量
        while (_eventQueue.Count > _maxRecentEvents)
        {
            string oldKey = _eventQueue.Dequeue();
            _recentEventIds.Remove(oldKey);
        }

        return true;
    }
}
```

## 与Unity引擎的结合点

### 1. MonoBehaviour集成

```csharp
using UnityEngine;
using Basement.Events;

public class EventDispatcher : MonoBehaviour
{
    private void Update()
    {
        // 更新事件调度器
        EventScheduler.Instance.Update();
    }

    private void OnDestroy()
    {
        // 清理事件总线
        EventBus.Instance.Clear();
    }
}
```

### 2. 编辑器工具

```csharp
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Basement.Events;

public class EventDebugWindow : EditorWindow
{
    [MenuItem("Tools/Event Debug")]
    public static void ShowWindow()
    {
        GetWindow<EventDebugWindow>("事件调试器");
    }

    private void OnGUI()
    {
        GUILayout.Label("事件统计", EditorStyles.boldLabel);

        GUILayout.Label($"队列大小: {EventScheduler.Instance.QueueSize}");
        GUILayout.Label($"PlayerDamageEvent 处理器数量: {EventBus.Instance.GetHandlerCount<PlayerDamageEvent>()}");

        GUILayout.Space(10);

        if (GUILayout.Button("清空事件队列"))
        {
            EventScheduler.Instance.ClearQueue();
        }

        if (GUILayout.Button("清空所有订阅"))
        {
            EventBus.Instance.Clear();
        }

        if (GUILayout.Button("导出事件历史"))
        {
            string filePath = EditorUtility.SaveFilePanel("导出事件历史", "", "EventHistory.csv", "csv");
            if (!string.IsNullOrEmpty(filePath))
            {
                EventHistoryRecorder.Instance.ExportRecords(filePath);
            }
        }
    }
}
#endif
```

### 3. 协程支持

```csharp
using UnityEngine;
using Basement.Events;
using System.Collections;

public class CoroutineEventExample : MonoBehaviour
{
    public IEnumerator WaitForEvent<T>() where T : IEvent
    {
        bool eventReceived = false;
        T receivedEvent = default;

        void OnEvent(T eventData)
        {
            eventReceived = true;
            receivedEvent = eventData;
            EventBus.Instance.Unsubscribe(OnEvent);
        }

        EventBus.Instance.Subscribe(OnEvent);

        yield return new WaitUntil(() => eventReceived);

        Debug.Log($"收到事件: {receivedEvent.EventId}");
    }
}
```

## 总结

事件调度服务通过观察者模式和优先级队列实现，提供了高效、灵活的事件通信机制，具有以下优势：

1. **解耦通信**：实现模块间的松耦合通信，降低系统复杂度
2. **高性能**：优化事件分发性能，支持大量事件的实时处理
3. **灵活性**：支持多种事件类型、优先级、过滤条件等
4. **可靠性**：确保事件不丢失，支持事件重试和错误处理
5. **易于扩展**：支持自定义事件类型和处理器

通过使用事件调度服务，项目可以实现模块间的高效通信，提升系统的可维护性和扩展性。
