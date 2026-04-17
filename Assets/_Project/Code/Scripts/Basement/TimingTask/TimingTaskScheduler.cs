using System;
using System.Collections.Generic;
using Basement.MatchTime;
using Basement.Utils;

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
        /// 更新调度器（任务计时仅在对局进行中且未暂停时递减，与 <see cref="MatchTimeService"/> 一致）。
        /// </summary>
        public void Update()
        {
            if (UnityEngine.Time.time - _lastUpdateTime < _updateInterval)
            {
                return;
            }

            float deltaTime = UnityEngine.Time.time - _lastUpdateTime;
            _lastUpdateTime = UnityEngine.Time.time;

            var match = MatchTimeService.Instance;
            if (!match.IsMatchActive || match.IsMatchPaused)
                deltaTime = 0f;

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
