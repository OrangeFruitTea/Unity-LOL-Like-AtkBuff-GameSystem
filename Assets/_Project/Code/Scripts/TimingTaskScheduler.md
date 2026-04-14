# 时序任务调度系统技术文档

## 概述

时序任务调度系统是核心业务层的核心组件，负责游戏中的定时任务管理。该系统支持延迟任务、周期性任务、定时任务、条件任务等多种任务类型，提供精确的时间管理和任务调度功能，确保游戏逻辑的时序正确性和性能优化。

## 模块架构设计

### 1. 设计目标

- **精确调度**：提供高精度的任务调度，支持毫秒级时间控制
- **多种任务类型**：支持延迟任务、周期性任务、定时任务、条件任务等
- **高性能**：优化任务调度性能，支持大量任务的实时管理
- **任务管理**：提供任务的创建、取消、暂停、恢复等管理功能
- **易于扩展**：支持自定义任务类型和调度策略

### 2. 架构分层

```
├── 时序任务调度系统
│   ├── 核心接口
│   │   ├── ITask (任务接口)
│   │   │   ├── TaskId
│   │   │   ├── State
│   │   │   ├── Priority
│   │   │   ├── CreatedTime
│   │   │   ├── ExecuteTime
│   │   │   ├── Execute()
│   │   │   ├── Cancel()
│   │   │   ├── Pause()
│   │   │   └── Resume()
│   ├── 核心组件
│   │   ├── TaskBase (任务基类)
│   │   │   ├── 基本属性实现
│   │   │   ├── 生命周期管理
│   │   │   └── 事件回调
│   │   ├── 任务类型
│   │   │   ├── DelayedTask (延迟任务)
│   │   │   │   ├── 延迟执行
│   │   │   │   └── 一次性执行
│   │   │   ├── PeriodicTask (周期性任务)
│   │   │   │   ├── 按间隔重复执行
│   │   │   │   └── 可设置重复次数
│   │   │   ├── ScheduledTask (定时任务)
│   │   │   │   ├── 在指定时间执行
│   │   │   │   └── 一次性执行
│   │   │   └── ConditionalTask (条件任务)
│   │   │       ├── 满足条件时执行
│   │   │       └── 定期检查条件
│   │   ├── TaskManager (任务管理器)
│   │   │   ├── 创建任务
│   │   │   ├── 管理任务
│   │   │   ├── 取消任务
│   │   │   ├── 暂停/恢复任务
│   │   │   └── 任务统计
│   │   └── TaskScheduler (任务调度器)
│   │       ├── 任务执行调度
│   │       ├── 时间管理
│   │       └── 活跃任务管理
│   ├── 辅助组件
│   │   ├── TaskPool<T> (任务池化)
│   │   ├── BatchTaskProcessor (批量任务处理)
│   │   └── PriorityTaskScheduler (任务优先级优化)
│   └── 与Unity集成
│       ├── MonoBehaviour集成
│       ├── 协程支持
│       ├── 编辑器工具
│       └── 时间缩放支持
```

### 3. 核心组件

#### 3.1 任务接口

```csharp
using System;

namespace Basement.Tasks
{
    /// <summary>
    /// 任务接口
    /// 定义任务的标准行为
    /// </summary>
    public interface ITask
    {
        /// <summary>
        /// 任务ID
        /// </summary>
        string TaskId { get; }

        /// <summary>
        /// 任务状态
        /// </summary>
        TaskState State { get; }

        /// <summary>
        /// 任务优先级
        /// </summary>
        TaskPriority Priority { get; }

        /// <summary>
        /// 任务创建时间
        /// </summary>
        DateTime CreatedTime { get; }

        /// <summary>
        /// 任务执行时间
        /// </summary>
        DateTime ExecuteTime { get; }

        /// <summary>
        /// 执行任务
        /// </summary>
        void Execute();

        /// <summary>
        /// 取消任务
        /// </summary>
        void Cancel();

        /// <summary>
        /// 暂停任务
        /// </summary>
        void Pause();

        /// <summary>
        /// 恢复任务
        /// </summary>
        void Resume();
    }

    /// <summary>
    /// 任务状态枚举
    /// </summary>
    public enum TaskState
    {
        /// <summary>
        /// 等待中
        /// </summary>
        Pending,

        /// <summary>
        /// 执行中
        /// </summary>
        Running,

        /// <summary>
        /// 已完成
        /// </summary>
        Completed,

        /// <summary>
        /// 已取消
        /// </summary>
        Cancelled,

        /// <summary>
        /// 已暂停
        /// </summary>
        Paused,

        /// <summary>
        /// 失败
        /// </summary>
        Failed
    }

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
```

#### 3.2 任务基类

```csharp
using System;
using Basement.Logging;

namespace Basement.Tasks
{
    /// <summary>
    /// 任务基类
    /// 提供任务的基本实现
    /// </summary>
    public abstract class TaskBase : ITask
    {
        public string TaskId { get; protected set; }
        public TaskState State { get; protected set; }
        public TaskPriority Priority { get; protected set; }
        public DateTime CreatedTime { get; protected set; }
        public DateTime ExecuteTime { get; protected set; }

        protected Action _onCompleted;
        protected Action _onCancelled;
        protected Action<Exception> _onFailed;

        protected TaskBase(string taskId, TaskPriority priority, float delay)
        {
            TaskId = taskId ?? Guid.NewGuid().ToString();
            Priority = priority;
            State = TaskState.Pending;
            CreatedTime = DateTime.Now;
            ExecuteTime = DateTime.Now.AddSeconds(delay);
        }

        public virtual void Execute()
        {
            if (State != TaskState.Pending)
            {
                LogManager.Instance.LogWarning($"任务状态不正确，无法执行 [TaskId: {TaskId}, State: {State}]", "TaskBase");
                return;
            }

            State = TaskState.Running;

            try
            {
                ExecuteInternal();
                State = TaskState.Completed;
                _onCompleted?.Invoke();
                LogManager.Instance.LogDebug($"任务执行成功 [TaskId: {TaskId}]", "TaskBase");
            }
            catch (Exception ex)
            {
                State = TaskState.Failed;
                _onFailed?.Invoke(ex);
                LogManager.Instance.LogError($"任务执行失败 [TaskId: {TaskId}]: {ex.Message}", "TaskBase");
            }
        }

        public virtual void Cancel()
        {
            if (State == TaskState.Completed || State == TaskState.Cancelled)
            {
                return;
            }

            State = TaskState.Cancelled;
            _onCancelled?.Invoke();
            LogManager.Instance.LogDebug($"任务已取消 [TaskId: {TaskId}]", "TaskBase");
        }

        public virtual void Pause()
        {
            if (State != TaskState.Running && State != TaskState.Pending)
            {
                LogManager.Instance.LogWarning($"任务状态不正确，无法暂停 [TaskId: {TaskId}, State: {State}]", "TaskBase");
                return;
            }

            State = TaskState.Paused;
            LogManager.Instance.LogDebug($"任务已暂停 [TaskId: {TaskId}]", "TaskBase");
        }

        public virtual void Resume()
        {
            if (State != TaskState.Paused)
            {
                LogManager.Instance.LogWarning($"任务状态不正确，无法恢复 [TaskId: {TaskId}, State: {State}]", "TaskBase");
                return;
            }

            State = TaskState.Pending;
            LogManager.Instance.LogDebug($"任务已恢复 [TaskId: {TaskId}]", "TaskBase");
        }

        protected abstract void ExecuteInternal();

        public TaskBase OnCompleted(Action callback)
        {
            _onCompleted += callback;
            return this;
        }

        public TaskBase OnCancelled(Action callback)
        {
            _onCancelled += callback;
            return this;
        }

        public TaskBase OnFailed(Action<Exception> callback)
        {
            _onFailed += callback;
            return this;
        }
    }
}
```

#### 3.3 延迟任务

```csharp
using System;
using Basement.Logging;

namespace Basement.Tasks
{
    /// <summary>
    /// 延迟任务
    /// 在指定延迟后执行的任务
    /// </summary>
    public class DelayedTask : TaskBase
    {
        private readonly Action _action;
        private readonly float _delay;

        public DelayedTask(Action action, float delay, TaskPriority priority = TaskPriority.Normal)
            : base(null, priority, delay)
        {
            _action = action ?? throw new ArgumentNullException(nameof(action));
            _delay = delay;
        }

        public DelayedTask(string taskId, Action action, float delay, TaskPriority priority = TaskPriority.Normal)
            : base(taskId, priority, delay)
        {
            _action = action ?? throw new ArgumentNullException(nameof(action));
            _delay = delay;
        }

        protected override void ExecuteInternal()
        {
            _action?.Invoke();
        }

        public override string ToString()
        {
            return $"DelayedTask [TaskId: {TaskId}, Delay: {_delay}s, Priority: {Priority}]";
        }
    }
}
```

#### 3.4 周期性任务

```csharp
using System;
using Basement.Logging;

namespace Basement.Tasks
{
    /// <summary>
    /// 周期性任务
    /// 按指定周期重复执行的任务
    /// </summary>
    public class PeriodicTask : TaskBase
    {
        private readonly Action _action;
        private readonly float _interval;
        private readonly int _repeatCount;
        private int _currentRepeat;
        private DateTime _lastExecuteTime;

        public PeriodicTask(Action action, float interval, int repeatCount = -1, TaskPriority priority = TaskPriority.Normal)
            : base(null, priority, interval)
        {
            _action = action ?? throw new ArgumentNullException(nameof(action));
            _interval = interval;
            _repeatCount = repeatCount;
            _currentRepeat = 0;
            _lastExecuteTime = DateTime.Now;
        }

        public PeriodicTask(string taskId, Action action, float interval, int repeatCount = -1, TaskPriority priority = TaskPriority.Normal)
            : base(taskId, priority, interval)
        {
            _action = action ?? throw new ArgumentNullException(nameof(action));
            _interval = interval;
            _repeatCount = repeatCount;
            _currentRepeat = 0;
            _lastExecuteTime = DateTime.Now;
        }

        public override void Execute()
        {
            if (State != TaskState.Pending)
            {
                return;
            }

            State = TaskState.Running;

            try
            {
                _action?.Invoke();
                _currentRepeat++;
                _lastExecuteTime = DateTime.Now;

                if (_repeatCount > 0 && _currentRepeat >= _repeatCount)
                {
                    State = TaskState.Completed;
                    _onCompleted?.Invoke();
                    LogManager.Instance.LogDebug($"周期任务完成 [TaskId: {TaskId}, 重复次数: {_currentRepeat}]", "PeriodicTask");
                }
                else
                {
                    State = TaskState.Pending;
                    ExecuteTime = DateTime.Now.AddSeconds(_interval);
                    LogManager.Instance.LogDebug($"周期任务继续 [TaskId: {TaskId}, 下次执行: {ExecuteTime}]", "PeriodicTask");
                }
            }
            catch (Exception ex)
            {
                State = TaskState.Failed;
                _onFailed?.Invoke(ex);
                LogManager.Instance.LogError($"周期任务执行失败 [TaskId: {TaskId}]: {ex.Message}", "PeriodicTask");
            }
        }

        protected override void ExecuteInternal()
        {
            _action?.Invoke();
        }

        public int CurrentRepeat => _currentRepeat;
        public int RepeatCount => _repeatCount;
        public float Interval => _interval;

        public override string ToString()
        {
            return $"PeriodicTask [TaskId: {TaskId}, Interval: {_interval}s, Repeat: {_currentRepeat}/{_repeatCount}]";
        }
    }
}
```

#### 3.5 定时任务

```csharp
using System;
using Basement.Logging;

namespace Basement.Tasks
{
    /// <summary>
    /// 定时任务
    /// 在指定时间执行的任务
    /// </summary>
    public class ScheduledTask : TaskBase
    {
        private readonly Action _action;
        private readonly DateTime _scheduledTime;

        public ScheduledTask(Action action, DateTime scheduledTime, TaskPriority priority = TaskPriority.Normal)
            : base(null, priority, (float)(scheduledTime - DateTime.Now).TotalSeconds)
        {
            _action = action ?? throw new ArgumentNullException(nameof(action));
            _scheduledTime = scheduledTime;
            ExecuteTime = scheduledTime;
        }

        public ScheduledTask(string taskId, Action action, DateTime scheduledTime, TaskPriority priority = TaskPriority.Normal)
            : base(taskId, priority, (float)(scheduledTime - DateTime.Now).TotalSeconds)
        {
            _action = action ?? throw new ArgumentNullException(nameof(action));
            _scheduledTime = scheduledTime;
            ExecuteTime = scheduledTime;
        }

        protected override void ExecuteInternal()
        {
            _action?.Invoke();
        }

        public DateTime ScheduledTime => _scheduledTime;

        public override string ToString()
        {
            return $"ScheduledTask [TaskId: {TaskId}, ScheduledTime: {_scheduledTime}]";
        }
    }
}
```

#### 3.6 条件任务

```csharp
using System;
using Basement.Logging;

namespace Basement.Tasks
{
    /// <summary>
    /// 条件任务
    /// 当满足指定条件时执行的任务
    /// </summary>
    public class ConditionalTask : TaskBase
    {
        private readonly Action _action;
        private readonly Func<bool> _condition;
        private readonly float _checkInterval;
        private DateTime _lastCheckTime;

        public ConditionalTask(Action action, Func<bool> condition, float checkInterval = 0.1f, TaskPriority priority = TaskPriority.Normal)
            : base(null, priority, checkInterval)
        {
            _action = action ?? throw new ArgumentNullException(nameof(action));
            _condition = condition ?? throw new ArgumentNullException(nameof(condition));
            _checkInterval = checkInterval;
            _lastCheckTime = DateTime.Now;
        }

        public ConditionalTask(string taskId, Action action, Func<bool> condition, float checkInterval = 0.1f, TaskPriority priority = TaskPriority.Normal)
            : base(taskId, priority, checkInterval)
        {
            _action = action ?? throw new ArgumentNullException(nameof(action));
            _condition = condition ?? throw new ArgumentNullException(nameof(condition));
            _checkInterval = checkInterval;
            _lastCheckTime = DateTime.Now;
        }

        public override void Execute()
        {
            if (State != TaskState.Pending)
            {
                return;
            }

            _lastCheckTime = DateTime.Now;

            try
            {
                if (_condition())
                {
                    State = TaskState.Running;
                    _action?.Invoke();
                    State = TaskState.Completed;
                    _onCompleted?.Invoke();
                    LogManager.Instance.LogDebug($"条件任务完成 [TaskId: {TaskId}]", "ConditionalTask");
                }
                else
                {
                    ExecuteTime = DateTime.Now.AddSeconds(_checkInterval);
                }
            }
            catch (Exception ex)
            {
                State = TaskState.Failed;
                _onFailed?.Invoke(ex);
                LogManager.Instance.LogError($"条件任务执行失败 [TaskId: {TaskId}]: {ex.Message}", "ConditionalTask");
            }
        }

        protected override void ExecuteInternal()
        {
            _action?.Invoke();
        }

        public float CheckInterval => _checkInterval;

        public override string ToString()
        {
            return $"ConditionalTask [TaskId: {TaskId}, CheckInterval: {_checkInterval}s]";
        }
    }
}
```

#### 3.7 任务管理器

```csharp
using System;
using System.Collections.Generic;
using Basement.Logging;
using Basement.Utils;

namespace Basement.Tasks
{
    /// <summary>
    /// 任务管理器
    /// 负责任务的创建、管理和调度
    /// </summary>
    public class TaskManager : Singleton<TaskManager>
    {
        private readonly Dictionary<string, ITask> _tasks = new Dictionary<string, ITask>();
        private readonly PriorityQueue<ITask> _taskQueue = new PriorityQueue<ITask>();
        private readonly object _lock = new object();
        private bool _isInitialized = false;
        private bool _isProcessing = false;
        private int _maxConcurrentTasks = 10;

        public void Initialize()
        {
            if (_isInitialized) return;

            _isInitialized = true;
            LogManager.Instance.LogInfo("任务管理器初始化完成", "TaskManager");
        }

        /// <summary>
        /// 创建延迟任务
        /// </summary>
        public DelayedTask CreateDelayedTask(Action action, float delay, TaskPriority priority = TaskPriority.Normal)
        {
            DelayedTask task = new DelayedTask(action, delay, priority);
            RegisterTask(task);
            return task;
        }

        /// <summary>
        /// 创建周期性任务
        /// </summary>
        public PeriodicTask CreatePeriodicTask(Action action, float interval, int repeatCount = -1, TaskPriority priority = TaskPriority.Normal)
        {
            PeriodicTask task = new PeriodicTask(action, interval, repeatCount, priority);
            RegisterTask(task);
            return task;
        }

        /// <summary>
        /// 创建定时任务
        /// </summary>
        public ScheduledTask CreateScheduledTask(Action action, DateTime scheduledTime, TaskPriority priority = TaskPriority.Normal)
        {
            ScheduledTask task = new ScheduledTask(action, scheduledTime, priority);
            RegisterTask(task);
            return task;
        }

        /// <summary>
        /// 创建条件任务
        /// </summary>
        public ConditionalTask CreateConditionalTask(Action action, Func<bool> condition, float checkInterval = 0.1f, TaskPriority priority = TaskPriority.Normal)
        {
            ConditionalTask task = new ConditionalTask(action, condition, checkInterval, priority);
            RegisterTask(task);
            return task;
        }

        private void RegisterTask(ITask task)
        {
            if (task == null) return;

            lock (_lock)
            {
                _tasks[task.TaskId] = task;
                _taskQueue.Enqueue(task, (int)task.Priority);
                LogManager.Instance.LogDebug($"注册任务: {task}", "TaskManager");
            }
        }

        /// <summary>
        /// 取消任务
        /// </summary>
        public bool CancelTask(string taskId)
        {
            if (string.IsNullOrEmpty(taskId)) return false;

            lock (_lock)
            {
                if (_tasks.TryGetValue(taskId, out var task))
                {
                    task.Cancel();
                    _tasks.Remove(taskId);
                    LogManager.Instance.LogDebug($"取消任务: {taskId}", "TaskManager");
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 暂停任务
        /// </summary>
        public bool PauseTask(string taskId)
        {
            if (string.IsNullOrEmpty(taskId)) return false;

            lock (_lock)
            {
                if (_tasks.TryGetValue(taskId, out var task))
                {
                    task.Pause();
                    LogManager.Instance.LogDebug($"暂停任务: {taskId}", "TaskManager");
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 恢复任务
        /// </summary>
        public bool ResumeTask(string taskId)
        {
            if (string.IsNullOrEmpty(taskId)) return false;

            lock (_lock)
            {
                if (_tasks.TryGetValue(taskId, out var task))
                {
                    task.Resume();
                    LogManager.Instance.LogDebug($"恢复任务: {taskId}", "TaskManager");
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 获取任务
        /// </summary>
        public ITask GetTask(string taskId)
        {
            if (string.IsNullOrEmpty(taskId)) return null;

            lock (_lock)
            {
                return _tasks.TryGetValue(taskId, out var task) ? task : null;
            }
        }

        /// <summary>
        /// 获取所有任务
        /// </summary>
        public List<ITask> GetAllTasks()
        {
            lock (_lock)
            {
                return new List<ITask>(_tasks.Values);
            }
        }

        /// <summary>
        /// 清空所有任务
        /// </summary>
        public void ClearAllTasks()
        {
            lock (_lock)
            {
                foreach (var task in _tasks.Values)
                {
                    task.Cancel();
                }

                _tasks.Clear();
                _taskQueue.Clear();
                LogManager.Instance.LogInfo("清空所有任务", "TaskManager");
            }
        }

        /// <summary>
        /// 清空已完成任务
        /// </summary>
        public void ClearCompletedTasks()
        {
            lock (_lock)
            {
                List<string> completedTaskIds = new List<string>();

                foreach (var kvp in _tasks)
                {
                    if (kvp.Value.State == TaskState.Completed || kvp.Value.State == TaskState.Cancelled || kvp.Value.State == TaskState.Failed)
                    {
                        completedTaskIds.Add(kvp.Key);
                    }
                }

                foreach (string taskId in completedTaskIds)
                {
                    _tasks.Remove(taskId);
                }

                LogManager.Instance.LogInfo($"清空已完成任务: {completedTaskIds.Count}个", "TaskManager");
            }
        }

        /// <summary>
        /// 获取任务统计信息
        /// </summary>
        public TaskStatistics GetStatistics()
        {
            lock (_lock)
            {
                TaskStatistics stats = new TaskStatistics();

                foreach (var task in _tasks.Values)
                {
                    switch (task.State)
                    {
                        case TaskState.Pending:
                            stats.PendingCount++;
                            break;
                        case TaskState.Running:
                            stats.RunningCount++;
                            break;
                        case TaskState.Completed:
                            stats.CompletedCount++;
                            break;
                        case TaskState.Cancelled:
                            stats.CancelledCount++;
                            break;
                        case TaskState.Paused:
                            stats.PausedCount++;
                            break;
                        case TaskState.Failed:
                            stats.FailedCount++;
                            break;
                    }
                }

                stats.TotalCount = _tasks.Count;
                return stats;
            }
        }

        /// <summary>
        /// 设置最大并发任务数
        /// </summary>
        public void SetMaxConcurrentTasks(int max)
        {
            _maxConcurrentTasks = Math.Max(1, max);
            LogManager.Instance.LogInfo($"最大并发任务数设置为: {_maxConcurrentTasks}", "TaskManager");
        }
    }

    /// <summary>
    /// 任务统计信息
    /// </summary>
    public class TaskStatistics
    {
        public int TotalCount { get; set; }
        public int PendingCount { get; set; }
        public int RunningCount { get; set; }
        public int CompletedCount { get; set; }
        public int CancelledCount { get; set; }
        public int PausedCount { get; set; }
        public int FailedCount { get; set; }

        public override string ToString()
        {
            return $"TaskStatistics [Total: {TotalCount}, Pending: {PendingCount}, Running: {RunningCount}, Completed: {CompletedCount}, Cancelled: {CancelledCount}, Paused: {PausedCount}, Failed: {FailedCount}]";
        }
    }
}
```

#### 3.8 任务调度器

```csharp
using System;
using System.Collections.Generic;
using Basement.Logging;
using Basement.Utils;

namespace Basement.Tasks
{
    /// <summary>
    /// 任务调度器
    /// 负责任务的调度和执行
    /// </summary>
    public class TaskScheduler : Singleton<TaskScheduler>
    {
        private readonly List<ITask> _activeTasks = new List<ITask>();
        private readonly object _lock = new object();
        private bool _isInitialized = false;
        private float _updateInterval = 0.016f; // 约60FPS
        private float _lastUpdateTime = 0f;

        public void Initialize()
        {
            if (_isInitialized) return;

            _isInitialized = true;
            LogManager.Instance.LogInfo("任务调度器初始化完成", "TaskScheduler");
        }

        /// <summary>
        /// 更新调度器
        /// </summary>
        public void Update()
        {
            if (UnityEngine.Time.time - _lastUpdateTime < _updateInterval)
            {
                return;
            }

            _lastUpdateTime = UnityEngine.Time.time;

            ProcessTasks();
        }

        private void ProcessTasks()
        {
            lock (_lock)
            {
                var allTasks = TaskManager.Instance.GetAllTasks();
                DateTime currentTime = DateTime.Now;

                foreach (var task in allTasks)
                {
                    if (task.State == TaskState.Paused || task.State == TaskState.Cancelled || task.State == TaskState.Completed || task.State == TaskState.Failed)
                    {
                        continue;
                    }

                    if (task.ExecuteTime <= currentTime)
                    {
                        if (!_activeTasks.Contains(task))
                        {
                            _activeTasks.Add(task);
                            task.Execute();
                        }
                    }
                }

                // 清理已完成的任务
                _activeTasks.RemoveAll(t => t.State == TaskState.Completed || t.State == TaskState.Cancelled || t.State == TaskState.Failed);
            }
        }

        /// <summary>
        /// 设置更新间隔
        /// </summary>
        public void SetUpdateInterval(float interval)
        {
            _updateInterval = Math.Max(0.001f, interval);
            LogManager.Instance.LogInfo($"更新间隔设置为: {_updateInterval}秒", "TaskScheduler");
        }

        /// <summary>
        /// 获取活跃任务数量
        /// </summary>
        public int ActiveTaskCount
        {
            get
            {
                lock (_lock)
                {
                    return _activeTasks.Count;
                }
            }
        }
    }
}
```

## 使用说明

### 1. 基础使用

```csharp
using UnityEngine;
using Basement.Tasks;

public class TaskExample : MonoBehaviour
{
    private void Start()
    {
        // 初始化任务管理器和调度器
        TaskManager.Instance.Initialize();
        TaskScheduler.Instance.Initialize();
    }

    private void Update()
    {
        // 更新任务调度器
        TaskScheduler.Instance.Update();
    }

    public void CreateDelayedTask()
    {
        // 创建延迟任务
        TaskManager.Instance.CreateDelayedTask(() =>
        {
            Debug.Log("延迟任务执行了！");
        }, 3f);
    }

    public void CreatePeriodicTask()
    {
        // 创建周期性任务（每1秒执行一次，共执行5次）
        TaskManager.Instance.CreatePeriodicTask(() =>
        {
            Debug.Log("周期性任务执行了！");
        }, 1f, 5);
    }

    public void CreateScheduledTask()
    {
        // 创建定时任务（在指定时间执行）
        DateTime scheduledTime = DateTime.Now.AddSeconds(10);
        TaskManager.Instance.CreateScheduledTask(() =>
        {
            Debug.Log("定时任务执行了！");
        }, scheduledTime);
    }

    public void CreateConditionalTask()
    {
        // 创建条件任务（当条件满足时执行）
        TaskManager.Instance.CreateConditionalTask(() =>
        {
            Debug.Log("条件任务执行了！");
        }, () =>
        {
            // 条件：当前时间超过指定时间
            return DateTime.Now.Second % 5 == 0;
        }, 0.1f);
    }
}
```

### 2. 任务链式调用

```csharp
using UnityEngine;
using Basement.Tasks;

public class TaskChainExample : MonoBehaviour
{
    public void CreateTaskWithCallbacks()
    {
        // 创建带回调的任务
        TaskManager.Instance.CreateDelayedTask(() =>
        {
            Debug.Log("任务执行中...");
        }, 2f)
        .OnCompleted(() =>
        {
            Debug.Log("任务完成！");
        })
        .OnCancelled(() =>
        {
            Debug.Log("任务已取消");
        })
        .OnFailed((ex) =>
        {
            Debug.LogError($"任务失败: {ex.Message}");
        });
    }

    public void CreateTaskChain()
    {
        // 创建任务链
        TaskManager.Instance.CreateDelayedTask(() =>
        {
            Debug.Log("第一步");
        }, 1f)
        .OnCompleted(() =>
        {
            TaskManager.Instance.CreateDelayedTask(() =>
            {
                Debug.Log("第二步");
            }, 1f)
            .OnCompleted(() =>
            {
                TaskManager.Instance.CreateDelayedTask(() =>
                {
                    Debug.Log("第三步");
                }, 1f);
            });
        });
    }
}
```

### 3. 任务管理

```csharp
using UnityEngine;
using Basement.Tasks;

public class TaskManagementExample : MonoBehaviour
{
    private string _taskId;

    public void CreateAndManageTask()
    {
        // 创建任务并保存任务ID
        DelayedTask task = TaskManager.Instance.CreateDelayedTask(() =>
        {
            Debug.Log("任务执行了！");
        }, 5f);

        _taskId = task.TaskId;
    }

    public void CancelTask()
    {
        // 取消任务
        if (!string.IsNullOrEmpty(_taskId))
        {
            TaskManager.Instance.CancelTask(_taskId);
            Debug.Log("任务已取消");
        }
    }

    public void PauseTask()
    {
        // 暂停任务
        if (!string.IsNullOrEmpty(_taskId))
        {
            TaskManager.Instance.PauseTask(_taskId);
            Debug.Log("任务已暂停");
        }
    }

    public void ResumeTask()
    {
        // 恢复任务
        if (!string.IsNullOrEmpty(_taskId))
        {
            TaskManager.Instance.ResumeTask(_taskId);
            Debug.Log("任务已恢复");
        }
    }

    public void ShowTaskStatistics()
    {
        // 显示任务统计信息
        TaskStatistics stats = TaskManager.Instance.GetStatistics();
        Debug.Log(stats.ToString());
    }

    public void ClearCompletedTasks()
    {
        // 清空已完成任务
        TaskManager.Instance.ClearCompletedTasks();
        Debug.Log("已清空已完成任务");
    }
}
```

### 4. 游戏场景应用

```csharp
using UnityEngine;
using Basement.Tasks;

public class GameSceneTaskExample : MonoBehaviour
{
    public void CreateGameTasks()
    {
        // 创建游戏加载任务
        TaskManager.Instance.CreateDelayedTask(() =>
        {
            Debug.Log("游戏资源加载完成");
        }, 2f);

        // 创建AI巡逻任务
        TaskManager.Instance.CreatePeriodicTask(() =>
        {
            UpdateAIPatrol();
        }, 5f, -1);

        // 创建技能冷却任务
        TaskManager.Instance.CreateDelayedTask(() =>
        {
            ResetSkillCooldown();
        }, 10f);

        // 创建倒计时任务
        TaskManager.Instance.CreatePeriodicTask(() =>
        {
            UpdateCountdown();
        }, 1f, 60);
    }

    private void UpdateAIPatrol()
    {
        Debug.Log("AI巡逻更新");
    }

    private void ResetSkillCooldown()
    {
        Debug.Log("技能冷却重置");
    }

    private void UpdateCountdown()
    {
        Debug.Log("倒计时更新");
    }
}
```

## 性能优化策略

### 1. 任务池化

```csharp
public class TaskPool<T> where T : ITask, new()
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

    public void Return(T task)
    {
        if (task == null) return;

        lock (_lock)
        {
            _pool.Push(task);
        }
    }
}
```

### 2. 批量任务处理

```csharp
public class BatchTaskProcessor
{
    private readonly List<ITask> _batch = new List<ITask>();
    private readonly int _batchSize = 50;
    private readonly float _batchInterval = 0.05f;
    private float _lastBatchTime = 0f;

    public void AddTask(ITask task)
    {
        _batch.Add(task);

        if (_batch.Count >= _batchSize ||
            UnityEngine.Time.time - _lastBatchTime >= _batchInterval)
        {
            ProcessBatch();
        }
    }

    private void ProcessBatch()
    {
        if (_batch.Count == 0) return;

        foreach (var task in _batch)
        {
            task.Execute();
        }

        _batch.Clear();
        _lastBatchTime = UnityEngine.Time.time;
    }
}
```

### 3. 任务优先级优化

```csharp
public class PriorityTaskScheduler
{
    private readonly Dictionary<TaskPriority, Queue<ITask>> _priorityQueues = new Dictionary<TaskPriority, Queue<ITask>>();

    public PriorityTaskScheduler()
    {
        foreach (TaskPriority priority in Enum.GetValues(typeof(TaskPriority)))
        {
            _priorityQueues[priority] = new Queue<ITask>();
        }
    }

    public void ScheduleTask(ITask task)
    {
        _priorityQueues[task.Priority].Enqueue(task);
    }

    public ITask GetNextTask()
    {
        // 按优先级从高到低获取任务
        foreach (TaskPriority priority in Enum.GetValues(typeof(TaskPriority)))
        {
            if (_priorityQueues[priority].Count > 0)
            {
                return _priorityQueues[priority].Dequeue();
            }
        }

        return null;
    }
}
```

## 与Unity引擎的结合点

### 1. MonoBehaviour集成

```csharp
using UnityEngine;
using Basement.Tasks;

public class TaskDispatcher : MonoBehaviour
{
    private void Update()
    {
        // 更新任务调度器
        TaskScheduler.Instance.Update();
    }

    private void OnDestroy()
    {
        // 清理所有任务
        TaskManager.Instance.ClearAllTasks();
    }
}
```

### 2. 协程支持

```csharp
using UnityEngine;
using Basement.Tasks;
using System.Collections;

public class CoroutineTaskExample : MonoBehaviour
{
    public IEnumerator WaitForTaskCompletion(string taskId)
    {
        bool taskCompleted = false;

        TaskManager.Instance.GetTask(taskId).OnCompleted(() =>
        {
            taskCompleted = true;
        });

        yield return new WaitUntil(() => taskCompleted);

        Debug.Log("任务完成");
    }

    public IEnumerator WaitForTaskDelay(float delay)
    {
        bool delayComplete = false;

        TaskManager.Instance.CreateDelayedTask(() =>
        {
            delayComplete = true;
        }, delay);

        yield return new WaitUntil(() => delayComplete);

        Debug.Log("延迟完成");
    }
}
```

### 3. 编辑器工具

```csharp
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Basement.Tasks;

public class TaskDebugWindow : EditorWindow
{
    [MenuItem("Tools/Task Debug")]
    public static void ShowWindow()
    {
        GetWindow<TaskDebugWindow>("任务调试器");
    }

    private void OnGUI()
    {
        GUILayout.Label("任务统计", EditorStyles.boldLabel);

        TaskStatistics stats = TaskManager.Instance.GetStatistics();
        GUILayout.Label($"总任务数: {stats.TotalCount}");
        GUILayout.Label($"等待中: {stats.PendingCount}");
        GUILayout.Label($"执行中: {stats.RunningCount}");
        GUILayout.Label($"已完成: {stats.CompletedCount}");
        GUILayout.Label($"已取消: {stats.CancelledCount}");
        GUILayout.Label($"已暂停: {stats.PausedCount}");
        GUILayout.Label($"失败: {stats.FailedCount}");

        GUILayout.Space(10);

        if (GUILayout.Button("清空已完成任务"))
        {
            TaskManager.Instance.ClearCompletedTasks();
        }

        if (GUILayout.Button("清空所有任务"))
        {
            TaskManager.Instance.ClearAllTasks();
        }
    }
}
#endif
```

### 4. 时间缩放支持

```csharp
using UnityEngine;
using Basement.Tasks;

public class TimeScaledTaskScheduler : TaskScheduler
{
    private float _timeScale = 1f;

    public void SetTimeScale(float scale)
    {
        _timeScale = Mathf.Clamp(scale, 0.1f, 10f);
    }

    public override void Update()
    {
        if (UnityEngine.Time.timeScale * _timeScale <= 0)
        {
            return;
        }

        float deltaTime = UnityEngine.Time.deltaTime * _timeScale;

        // 使用缩放后的时间更新任务
        ProcessTasks(deltaTime);
    }

    private void ProcessTasks(float deltaTime)
    {
        // 实现基于缩放时间的任务处理
    }
}
```

## 总结

时序任务调度系统通过支持多种任务类型和精确的时间管理，为游戏提供了强大的任务调度能力，具有以下优势：

1. **精确调度**：提供高精度的任务调度，支持毫秒级时间控制
2. **多种任务类型**：支持延迟任务、周期性任务、定时任务、条件任务等
3. **高性能**：优化任务调度性能，支持大量任务的实时管理
4. **任务管理**：提供任务的创建、取消、暂停、恢复等管理功能
5. **易于扩展**：支持自定义任务类型和调度策略

通过使用时序任务调度系统，项目可以实现高效的任务管理，提升游戏逻辑的可维护性和扩展性。
