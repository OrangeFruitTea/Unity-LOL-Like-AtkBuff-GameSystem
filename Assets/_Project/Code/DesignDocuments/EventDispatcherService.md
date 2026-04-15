# 事件调度服务技术文档

## 概述

事件调度服务是核心业务层的核心组件，负责游戏内事件的分发、订阅和处理。该服务采用观察者模式实现，支持同步和异步事件处理、事件优先级、静态事件过滤、可配置的事件历史记录等功能，为游戏系统提供高效、灵活的事件通信机制。

## 模块架构设计

### 1. 设计目标

- **解耦通信**：实现模块间的松耦合通信，降低系统复杂度
- **高性能**：优化事件分发性能，支持大量事件的实时处理
- **灵活性**：支持多种事件类型、优先级、静态过滤条件等
- **可靠性**：确保事件不丢失，支持错误处理
- **易于扩展**：支持自定义事件类型和处理器

### 2. 架构分层

```
├── 事件调度服务
│   ├── 核心接口
│   │   ├── IGameEvent (事件接口)
│   │   │   ├── EventId
│   │   │   ├── Timestamp
│   │   │   └── Priority
│   │   └── IGameEventHandler<T> (事件处理器接口)
│   │       ├── Handle(eventData)
│   │       └── Priority
│   ├── 核心组件
│   │   ├── GameEventBus (事件总线)
│   │   │   ├── 订阅事件 (Subscribe)
│   │   │   ├── 取消订阅 (Unsubscribe)
│   │   │   ├── 发布事件 (Publish)
│   │   │   ├── 异步发布 (PublishAsync)
│   │   │   └── 清空订阅 (Clear)
│   │   ├── GameEventScheduler (事件调度器)
│   │   │   ├── 调度事件 (Schedule)
│   │   │   ├── 延迟调度 (ScheduleDelayed)
│   │   │   ├── 处理队列 (ProcessQueue)
│   │   │   ├── 更新 (Update)
│   │   │   └── 清空队列 (ClearQueue)
│   │   └── GameEventHistory (事件历史记录)
│   │       ├── 记录事件 (RecordEvent)
│   │       ├── 获取记录 (GetRecords)
│   │       └── 清空记录 (Clear)
│   ├── 辅助组件
│   │   ├── PriorityQueue<T> (优先级队列)
│   │   └── EventPool<T> (事件池化)
│   └── 与Unity集成
│       ├── MonoBehaviour集成
│       ├── 编辑器工具
│       └── 协程支持
```

### 3. 核心组件

#### 3.1 事件接口

```csharp
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
```

#### 3.3 事件总线

```csharp
using System;
using System.Collections.Generic;
using System.Linq;

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
```

#### 3.4 事件调度器

```csharp
using System;
using System.Collections.Generic;

namespace Basement.Events
{
    /// <summary>
    /// 事件调度器
    /// 负责事件的队列管理和调度
    /// </summary>
    public class GameEventScheduler : Singleton<GameEventScheduler>
    {
        private readonly PriorityQueue<IGameEvent> _eventQueue = new PriorityQueue<IGameEvent>();
        private readonly List<IGameEvent> _processingEvents = new List<IGameEvent>();
        private readonly object _lock = new object();
        private bool _isProcessing = false;
        private int _maxConcurrentEvents = 10;
        private float _processInterval = 0.016f; // 约60FPS
        private float _lastProcessTime = 0f;

        public void Initialize()
        {
        }

        /// <summary>
        /// 调度事件
        /// </summary>
        /// <param name="eventData">事件数据</param>
        public void Schedule(IGameEvent eventData)
        {
            if (eventData == null)
            {
                return;
            }

            lock (_lock)
            {
                _eventQueue.Enqueue(eventData, (int)eventData.Priority);
            }
        }

        /// <summary>
        /// 延迟调度事件
        /// </summary>
        /// <param name="eventData">事件数据</param>
        /// <param name="delay">延迟时间（秒）</param>
        public void ScheduleDelayed(IGameEvent eventData, float delay)
        {
            if (eventData == null)
            {
                return;
            }

            // 使用Unity协程实现延迟
            UnityEngine.MonoBehaviour monoBehaviour = UnityEngine.Object.FindObjectOfType<UnityEngine.MonoBehaviour>();
            if (monoBehaviour != null)
            {
                monoBehaviour.StartCoroutine(ScheduleDelayedCoroutine(eventData, delay));
            }
        }

        private System.Collections.IEnumerator ScheduleDelayedCoroutine(IGameEvent eventData, float delay)
        {
            yield return new UnityEngine.WaitForSeconds(delay);
            Schedule(eventData);
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
                    IGameEvent eventData;

                    lock (_lock)
                    {
                        if (_eventQueue.Count == 0) break;

                        eventData = _eventQueue.Dequeue();
                        _processingEvents.Add(eventData);
                    }

                    try
                    {
                        GameEventBus.Instance.Publish(eventData);
                        processedCount++;
                    }
                    catch (Exception ex)
                    {
                        // 处理异常
                    }
                    finally
                    {
                        lock (_lock)
                        {
                            _processingEvents.Remove(eventData);
                        }
                    }
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
        }

        /// <summary>
        /// 设置处理间隔
        /// </summary>
        public void SetProcessInterval(float interval)
        {
            _processInterval = Math.Max(0.001f, interval);
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

#### 3.5 事件历史记录

```csharp
using System;
using System.Collections.Generic;

namespace Basement.Events
{
    /// <summary>
    /// 事件历史记录器
    /// 负责记录事件的历史信息
    /// </summary>
    public class GameEventHistory : Singleton<GameEventHistory>
    {
        private readonly Queue<GameEventRecord> _eventRecords = new Queue<GameEventRecord>();
        private readonly object _lock = new object();
        private int _maxRecords = 1000;
        private bool _isEnabled = false;

        public void Initialize(bool enabled = false, int maxRecords = 1000)
        {
            _maxRecords = maxRecords;
            _isEnabled = enabled;
        }

        /// <summary>
        /// 记录事件
        /// </summary>
        /// <param name="eventData">事件数据</param>
        public void RecordEvent(IGameEvent eventData)
        {
            if (!_isEnabled || eventData == null) return;

            lock (_lock)
            {
                GameEventRecord record = new GameEventRecord
                {
                    EventId = eventData.EventId,
                    EventType = eventData.GetType().Name,
                    Timestamp = eventData.Timestamp,
                    Priority = eventData.Priority
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
        public List<GameEventRecord> GetRecords(int count = 10)
        {
            lock (_lock)
            {
                List<GameEventRecord> records = new List<GameEventRecord>();

                foreach (var record in _eventRecords)
                {
                    records.Add(record);
                    if (records.Count >= count) break;
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
            }
        }

        /// <summary>
        /// 启用记录
        /// </summary>
        public void Enable()
        {
            _isEnabled = true;
        }

        /// <summary>
        /// 禁用记录
        /// </summary>
        public void Disable()
        {
            _isEnabled = false;
        }
    }

    /// <summary>
    /// 事件记录
    /// </summary>
    public class GameEventRecord
    {
        public string EventId { get; set; }
        public string EventType { get; set; }
        public DateTime Timestamp { get; set; }
        public GameEventPriority Priority { get; set; }
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
