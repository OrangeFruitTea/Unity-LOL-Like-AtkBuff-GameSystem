using System;
using System.Collections.Generic;
using Basement.Utils;

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
