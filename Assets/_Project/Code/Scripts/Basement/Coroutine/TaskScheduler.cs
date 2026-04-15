using System;
using System.Collections.Generic;
using System.Linq;
using Basement.Utils;
using UnityEngine;

namespace Basement.Threading
{
    /// <summary>
    /// 任务调度器（核心组件）
    /// </summary>
    public class TaskScheduler : Singleton<TaskScheduler>
    {
        private readonly Dictionary<string, ITask> _tasks = new Dictionary<string, ITask>();
        private readonly List<ITask> _readyTasks = new List<ITask>();
        private readonly List<ITask> _runningTasks = new List<ITask>();
        private readonly object _lock = new object();
        private bool _isInitialized = false;

        public void Initialize()
        {
            if (_isInitialized) return;

            _isInitialized = true;
            Debug.Log("任务调度器初始化完成");
        }

        /// <summary>
        /// 创建任务
        /// </summary>
        public ITask CreateTask(Action action, TaskPriority priority = TaskPriority.Normal, string taskId = null)
        {
            lock (_lock)
            {
                var task = new ActionTask(action, priority, taskId);
                _tasks[task.Id] = task;
                return task;
            }
        }

        /// <summary>
        /// 提交任务
        /// </summary>
        public void SubmitTask(ITask task)
        {
            lock (_lock)
            {
                if (!_tasks.ContainsKey(task.Id))
                {
                    _tasks[task.Id] = task;
                }
                
                task.OnStatusChanged += OnTaskStatusChanged;
                task.UpdateStatus();
                
                if (task.Status == TaskStatus.Ready)
                {
                    _readyTasks.Add(task);
                }
            }
        }

        /// <summary>
        /// 更新调度器
        /// </summary>
        public void Update()
        {
            lock (_lock)
            {
                ProcessReadyTasks();
                ProcessRunningTasks();
            }
        }

        private void ProcessReadyTasks()
        {
            for (int i = _readyTasks.Count - 1; i >= 0; i--)
            {
                var task = _readyTasks[i];
                
                if (task.Status == TaskStatus.Ready)
                {
                    _readyTasks.RemoveAt(i);
                    _runningTasks.Add(task);
                    
                    ThreadPool.Instance.SubmitTask(task);
                }
            }
        }

        private void ProcessRunningTasks()
        {
            for (int i = _runningTasks.Count - 1; i >= 0; i--)
            {
                var task = _runningTasks[i];
                
                if (task.Status == TaskStatus.Completed || 
                    task.Status == TaskStatus.Failed || 
                    task.Status == TaskStatus.Canceled)
                {
                    _runningTasks.RemoveAt(i);
                    HandleTaskCompletion(task);
                }
            }
        }

        private void HandleTaskCompletion(ITask completedTask)
        {
            // 通知后继任务更新状态
            foreach (var dependent in completedTask.Dependents)
            {
                dependent.UpdateStatus();
                
                if (dependent.Status == TaskStatus.Ready)
                {
                    _readyTasks.Add(dependent);
                }
            }
        }

        private void OnTaskStatusChanged(ITask task, TaskStatus oldStatus, TaskStatus newStatus)
        {
            lock (_lock)
            {
                if (oldStatus == TaskStatus.Waiting && newStatus == TaskStatus.Ready)
                {
                    if (!_readyTasks.Contains(task))
                    {
                        _readyTasks.Add(task);
                    }
                }
                else if (oldStatus == TaskStatus.Ready && newStatus == TaskStatus.Running)
                {
                    if (_readyTasks.Contains(task))
                    {
                        _readyTasks.Remove(task);
                    }
                    if (!_runningTasks.Contains(task))
                    {
                        _runningTasks.Add(task);
                    }
                }
                else if (newStatus == TaskStatus.Completed || 
                         newStatus == TaskStatus.Failed || 
                         newStatus == TaskStatus.Canceled)
                {
                    if (_readyTasks.Contains(task))
                    {
                        _readyTasks.Remove(task);
                    }
                    if (_runningTasks.Contains(task))
                    {
                        _runningTasks.Remove(task);
                    }
                }
            }
        }

        /// <summary>
        /// 获取任务
        /// </summary>
        public ITask GetTask(string taskId)
        {
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
        /// 取消任务
        /// </summary>
        public bool CancelTask(string taskId)
        {
            lock (_lock)
            {
                if (_tasks.TryGetValue(taskId, out var task))
                {
                    task.Status = TaskStatus.Canceled;
                    return true;
                }
                return false;
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
                    task.Status = TaskStatus.Canceled;
                }
                
                _tasks.Clear();
                _readyTasks.Clear();
                _runningTasks.Clear();
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
                    switch (task.Status)
                    {
                        case TaskStatus.Created:
                            stats.CreatedCount++;
                            break;
                        case TaskStatus.Waiting:
                            stats.WaitingCount++;
                            break;
                        case TaskStatus.Ready:
                            stats.ReadyCount++;
                            break;
                        case TaskStatus.Running:
                            stats.RunningCount++;
                            break;
                        case TaskStatus.Completed:
                            stats.CompletedCount++;
                            break;
                        case TaskStatus.Failed:
                            stats.FailedCount++;
                            break;
                        case TaskStatus.Canceled:
                            stats.CanceledCount++;
                            break;
                    }
                }

                stats.TotalCount = _tasks.Count;
                return stats;
            }
        }
    }
}
