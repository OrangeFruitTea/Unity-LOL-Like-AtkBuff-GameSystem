# 时序任务调度系统技术文档

## 概述

时序任务调度系统是核心业务层的核心组件，负责游戏中的定时任务管理。该系统支持时间触发任务、周期性任务、条件任务等多种任务类型，提供精确的时间管理和任务调度功能，确保游戏逻辑的时序正确性和性能优化。

## 模块架构设计

### 1. 设计目标

- **精确调度**：提供高精度的任务调度，支持毫秒级时间控制
- **多种任务类型**：支持时间触发任务、周期性任务、条件任务等
- **高性能**：优化任务调度性能，支持大量任务的实时管理
- **任务管理**：提供任务的创建、取消等管理功能
- **易于扩展**：支持自定义任务类型和调度策略

### 2. 架构分层

```
├── 时序任务调度系统
│   ├── 核心接口
│   │   ├── ITimingTask (任务接口)
│   │   │   ├── TaskId
│   │   │   ├── State
│   │   │   ├── Priority
│   │   │   ├── DelayTime
│   │   │   ├── Execute()
│   │   │   └── Cancel()
│   ├── 核心组件
│   │   ├── TimingTaskBase (任务基类)
│   │   │   ├── 基本属性实现
│   │   │   ├── 生命周期管理
│   │   │   └── 事件回调
│   │   ├── 任务类型
│   │   │   ├── TimeTriggeredTask (时间触发任务)
│   │   │   │   ├── 延迟执行
│   │   │   │   ├── 定时执行
│   │   │   │   └── 一次性执行
│   │   │   ├── PeriodicTask (周期性任务)
│   │   │   │   ├── 按间隔重复执行
│   │   │   │   └── 可设置重复次数
│   │   │   └── ConditionalTask (条件任务)
│   │   │       ├── 满足条件时执行
│   │   │       └── 定期检查条件
│   │   ├── TimingTaskManager (任务管理器)
│   │   │   ├── 创建任务
│   │   │   ├── 管理任务
│   │   │   ├── 取消任务
│   │   │   └── 任务统计
│   │   └── TimingTaskScheduler (任务调度器)
│   │       ├── 任务执行调度
│   │       ├── 时间管理
│   │       └── 活跃任务管理
│   ├── 辅助组件
│   │   ├── TaskPool<T> (任务池化)
│   │   └── BatchTaskProcessor (批量任务处理)
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
```

#### 3.2 任务基类

```csharp
using System;

namespace Basement.Tasks
{
    /// <summary>
    /// 任务基类
    /// 提供任务的基本实现
    /// </summary>
    public abstract class TimingTaskBase : ITimingTask
    {
        public string TaskId { get; protected set; }
        public TimingTaskState State { get; protected set; }
        public TimingTaskPriority Priority { get; protected set; }
        public float DelayTime { get; protected set; }

        protected Action _onCompleted;
        protected Action _onCancelled;
        protected Action<Exception> _onFailed;

        protected TimingTaskBase(string taskId, TimingTaskPriority priority, float delayTime)
        {
            TaskId = taskId ?? Guid.NewGuid().ToString();
            Priority = priority;
            State = TimingTaskState.Ready;
            DelayTime = delayTime;
        }

        public virtual void Execute()
        {
            if (State != TimingTaskState.Ready)
            {
                return;
            }

            State = TimingTaskState.Running;

            try
            {
                ExecuteInternal();
                State = TimingTaskState.Completed;
                _onCompleted?.Invoke();
            }
            catch (Exception ex)
            {
                State = TimingTaskState.Completed;
                _onFailed?.Invoke(ex);
            }
        }

        public virtual void Cancel()
        {
            if (State == TimingTaskState.Completed)
            {
                return;
            }

            State = TimingTaskState.Completed;
            _onCancelled?.Invoke();
        }

        protected abstract void ExecuteInternal();

        public TimingTaskBase OnCompleted(Action callback)
        {
            _onCompleted += callback;
            return this;
        }

        public TimingTaskBase OnCancelled(Action callback)
        {
            _onCancelled += callback;
            return this;
        }

        public TimingTaskBase OnFailed(Action<Exception> callback)
        {
            _onFailed += callback;
            return this;
        }
    }
}
```

#### 3.3 时间触发任务

```csharp
using System;

namespace Basement.Tasks
{
    /// <summary>
    /// 时间触发任务
    /// 在指定时间或延迟后执行的任务
    /// </summary>
    public class TimeTriggeredTask : TimingTaskBase
    {
        private readonly Action _action;

        public TimeTriggeredTask(Action action, float delayTime, TimingTaskPriority priority = TimingTaskPriority.Normal)
            : base(null, priority, delayTime)
        {
            _action = action ?? throw new ArgumentNullException(nameof(action));
        }

        public TimeTriggeredTask(string taskId, Action action, float delayTime, TimingTaskPriority priority = TimingTaskPriority.Normal)
            : base(taskId, priority, delayTime)
        {
            _action = action ?? throw new ArgumentNullException(nameof(action));
        }

        /// <summary>
        /// 创建基于绝对时间的时间触发任务
        /// </summary>
        public static TimeTriggeredTask CreateScheduledTask(Action action, DateTime scheduledTime, TimingTaskPriority priority = TimingTaskPriority.Normal)
        {
            float delayTime = (float)(scheduledTime - DateTime.Now).TotalSeconds;
            return new TimeTriggeredTask(action, Math.Max(0, delayTime), priority);
        }

        protected override void ExecuteInternal()
        {
            _action?.Invoke();
        }

        public override string ToString()
        {
            return $"TimeTriggeredTask [TaskId: {TaskId}, Delay: {DelayTime}s, Priority: {Priority}]";
        }
    }
}
```

#### 3.4 周期性任务

```csharp
using System;

namespace Basement.Tasks
{
    /// <summary>
    /// 周期性任务
    /// 按指定周期重复执行的任务
    /// </summary>
    public class PeriodicTask : TimingTaskBase
    {
        private readonly Action _action;
        private readonly int _repeatCount;
        private int _currentRepeat;

        public PeriodicTask(Action action, float interval, int repeatCount = -1, TimingTaskPriority priority = TimingTaskPriority.Normal)
            : base(null, priority, interval)
        {
            _action = action ?? throw new ArgumentNullException(nameof(action));
            _repeatCount = repeatCount;
            _currentRepeat = 0;
        }

        public PeriodicTask(string taskId, Action action, float interval, int repeatCount = -1, TimingTaskPriority priority = TimingTaskPriority.Normal)
            : base(taskId, priority, interval)
        {
            _action = action ?? throw new ArgumentNullException(nameof(action));
            _repeatCount = repeatCount;
            _currentRepeat = 0;
        }

        public override void Execute()
        {
            if (State != TimingTaskState.Ready)
            {
                return;
            }

            State = TimingTaskState.Running;

            try
            {
                _action?.Invoke();
                _currentRepeat++;

                if (_repeatCount > 0 && _currentRepeat >= _repeatCount)
                {
                    State = TimingTaskState.Completed;
                    _onCompleted?.Invoke();
                }
                else
                {
                    State = TimingTaskState.Ready;
                }
            }
            catch (Exception ex)
            {
                State = TimingTaskState.Completed;
                _onFailed?.Invoke(ex);
            }
        }

        protected override void ExecuteInternal()
        {
            _action?.Invoke();
        }

        public int CurrentRepeat => _currentRepeat;
        public int RepeatCount => _repeatCount;
        public float Interval => DelayTime;

        public override string ToString()
        {
            return $"PeriodicTask [TaskId: {TaskId}, Interval: {DelayTime}s, Repeat: {_currentRepeat}/{_repeatCount}]";
        }
    }
}
```

#### 3.5 条件任务

```csharp
using System;

namespace Basement.Tasks
{
    /// <summary>
    /// 条件任务
    /// 当满足指定条件时执行的任务
    /// </summary>
    public class ConditionalTask : TimingTaskBase
    {
        private readonly Action _action;
        private readonly Func<bool> _condition;

        public ConditionalTask(Action action, Func<bool> condition, float checkInterval = 0.1f, TimingTaskPriority priority = TimingTaskPriority.Normal)
            : base(null, priority, checkInterval)
        {
            _action = action ?? throw new ArgumentNullException(nameof(action));
            _condition = condition ?? throw new ArgumentNullException(nameof(condition));
        }

        public ConditionalTask(string taskId, Action action, Func<bool> condition, float checkInterval = 0.1f, TimingTaskPriority priority = TimingTaskPriority.Normal)
            : base(taskId, priority, checkInterval)
        {
            _action = action ?? throw new ArgumentNullException(nameof(action));
            _condition = condition ?? throw new ArgumentNullException(nameof(condition));
        }

        public override void Execute()
        {
            if (State != TimingTaskState.Ready)
            {
                return;
            }

            try
            {
                if (_condition())
                {
                    State = TimingTaskState.Running;
                    _action?.Invoke();
                    State = TimingTaskState.Completed;
                    _onCompleted?.Invoke();
                }
            }
            catch (Exception ex)
            {
                State = TimingTaskState.Completed;
                _onFailed?.Invoke(ex);
            }
        }

        protected override void ExecuteInternal()
        {
            _action?.Invoke();
        }

        public float CheckInterval => DelayTime;

        public override string ToString()
        {
            return $"ConditionalTask [TaskId: {TaskId}, CheckInterval: {DelayTime}s]";
        }
    }
}
```

#### 3.6 任务管理器

```csharp
using System;
using System.Collections.Generic;

namespace Basement.Tasks
{
    /// <summary>
    /// 任务管理器
    /// 负责任务的创建、管理和调度
    /// </summary>
    public class TimingTaskManager : Singleton<TimingTaskManager>
    {
        private readonly Dictionary<string, ITimingTask> _tasks = new Dictionary<string, ITimingTask>();
        private readonly List<ITimingTask> _taskQueue = new List<ITimingTask>();
        private readonly object _lock = new object();
        private bool _isInitialized = false;

        public void Initialize()
        {
            if (_isInitialized) return;

            _isInitialized = true;
        }

        /// <summary>
        /// 创建时间触发任务（延迟执行）
        /// </summary>
        public TimeTriggeredTask CreateTimeTriggeredTask(Action action, float delayTime, TimingTaskPriority priority = TimingTaskPriority.Normal)
        {
            TimeTriggeredTask task = new TimeTriggeredTask(action, delayTime, priority);
            RegisterTask(task);
            return task;
        }

        /// <summary>
        /// 创建时间触发任务（定时执行）
        /// </summary>
        public TimeTriggeredTask CreateScheduledTask(Action action, DateTime scheduledTime, TimingTaskPriority priority = TimingTaskPriority.Normal)
        {
            TimeTriggeredTask task = TimeTriggeredTask.CreateScheduledTask(action, scheduledTime, priority);
            RegisterTask(task);
            return task;
        }

        /// <summary>
        /// 创建周期性任务
        /// </summary>
        public PeriodicTask CreatePeriodicTask(Action action, float interval, int repeatCount = -1, TimingTaskPriority priority = TimingTaskPriority.Normal)
        {
            PeriodicTask task = new PeriodicTask(action, interval, repeatCount, priority);
            RegisterTask(task);
            return task;
        }

        /// <summary>
        /// 创建条件任务
        /// </summary>
        public ConditionalTask CreateConditionalTask(Action action, Func<bool> condition, float checkInterval = 0.1f, TimingTaskPriority priority = TimingTaskPriority.Normal)
        {
            ConditionalTask task = new ConditionalTask(action, condition, checkInterval, priority);
            RegisterTask(task);
            return task;
        }

        private void RegisterTask(ITimingTask task)
        {
            if (task == null) return;

            lock (_lock)
            {
                _tasks[task.TaskId] = task;
                _taskQueue.Add(task);
                // 按优先级和延迟时间排序
                _taskQueue.Sort((a, b) => {
                    int priorityComparison = b.Priority.CompareTo(a.Priority);
                    if (priorityComparison == 0)
                    {
                        return a.DelayTime.CompareTo(b.DelayTime);
                    }
                    return priorityComparison;
                });
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
                    _taskQueue.Remove(task);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 获取任务
        /// </summary>
        public ITimingTask GetTask(string taskId)
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
        public List<ITimingTask> GetAllTasks()
        {
            lock (_lock)
            {
                return new List<ITimingTask>(_tasks.Values);
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
                    if (kvp.Value.State == TimingTaskState.Completed)
                    {
                        completedTaskIds.Add(kvp.Key);
                    }
                }

                foreach (string taskId in completedTaskIds)
                {
                    _tasks.Remove(taskId);
                    _taskQueue.RemoveAll(t => t.TaskId == taskId);
                }
            }
        }

        /// <summary>
        /// 获取任务统计信息
        /// </summary>
        public TimingTaskStatistics GetStatistics()
        {
            lock (_lock)
            {
                TimingTaskStatistics stats = new TimingTaskStatistics();

                foreach (var task in _tasks.Values)
                {
                    switch (task.State)
                    {
                        case TimingTaskState.Ready:
                            stats.ReadyCount++;
                            break;
                        case TimingTaskState.Running:
                            stats.RunningCount++;
                            break;
                        case TimingTaskState.Completed:
                            stats.CompletedCount++;
                            break;
                    }
                }

                stats.TotalCount = _tasks.Count;
                return stats;
            }
        }
    }

    /// <summary>
    /// 任务统计信息
    /// </summary>
    public class TimingTaskStatistics
    {
        public int TotalCount { get; set; }
        public int ReadyCount { get; set; }
        public int RunningCount { get; set; }
        public int CompletedCount { get; set; }

        public override string ToString()
        {
            return $"TimingTaskStatistics [Total: {TotalCount}, Ready: {ReadyCount}, Running: {RunningCount}, Completed: {CompletedCount}]";
        }
    }
}
```

#### 3.7 任务调度器

```csharp
using System;
using System.Collections.Generic;

namespace Basement.Tasks
{
    /// <summary>
    /// 任务调度器
    /// 负责任务的调度和执行
    /// </summary>
    public class TimingTaskScheduler : Singleton<TimingTaskScheduler>
    {
        private readonly List<ITimingTask> _activeTasks = new List<ITimingTask>();
        private readonly Dictionary<ITimingTask, float> _taskTimers = new Dictionary<ITimingTask, float>();
        private readonly object _lock = new object();
        private bool _isInitialized = false;
        private float _updateInterval = 0.016f; // 约60FPS
        private float _lastUpdateTime = 0f;

        public void Initialize()
        {
            if (_isInitialized) return;

            _isInitialized = true;
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

            float deltaTime = UnityEngine.Time.time - _lastUpdateTime;
            _lastUpdateTime = UnityEngine.Time.time;

            ProcessTasks(deltaTime);
        }

        private void ProcessTasks(float deltaTime)
        {
            lock (_lock)
            {
                var allTasks = TimingTaskManager.Instance.GetAllTasks();

                // 更新任务计时器
                foreach (var task in allTasks)
                {
                    if (task.State == TimingTaskState.Ready)
                    {
                        if (!_taskTimers.ContainsKey(task))
                        {
                            _taskTimers[task] = task.DelayTime;
                        }

                        _taskTimers[task] -= deltaTime;

                        if (_taskTimers[task] <= 0)
                        {
                            if (!_activeTasks.Contains(task))
                            {
                                _activeTasks.Add(task);
                                task.Execute();

                                // 对于周期性任务，重置计时器
                                if (task is PeriodicTask periodicTask && periodicTask.State == TimingTaskState.Ready)
                                {
                                    _taskTimers[task] = periodicTask.Interval;
                                }
                                else if (task is ConditionalTask conditionalTask && conditionalTask.State == TimingTaskState.Ready)
                                {
                                    _taskTimers[task] = conditionalTask.CheckInterval;
                                }
                                else
                                {
                                    _taskTimers.Remove(task);
                                }
                            }
                        }
                    }
                    else if (task.State == TimingTaskState.Completed)
                    {
                        _taskTimers.Remove(task);
                    }
                }

                // 清理已完成的任务
                _activeTasks.RemoveAll(t => t.State == TimingTaskState.Completed);
            }
        }

        /// <summary>
        /// 设置更新间隔
        /// </summary>
        public void SetUpdateInterval(float interval)
        {
            _updateInterval = Math.Max(0.001f, interval);
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
