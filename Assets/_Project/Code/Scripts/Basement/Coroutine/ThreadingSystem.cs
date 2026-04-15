using System;
using System.Collections.Generic;
using UnityEngine;

namespace Basement.Threading
{
    /// <summary>
    /// 协程多线程系统主入口
    /// </summary>
    public class ThreadingSystem : MonoBehaviour
    {
        private TaskScheduler _taskScheduler;
        private CoroutineManager _coroutineManager;

        private void Awake()
        {
            Initialize();
        }

        private void Update()
        {
            _taskScheduler?.Update();
        }

        private void OnDestroy()
        {
            Cleanup();
        }

        /// <summary>
        /// 初始化系统
        /// </summary>
        public void Initialize()
        {
            // 初始化任务调度器
            _taskScheduler = TaskScheduler.Instance;
            _taskScheduler.Initialize();

            // 初始化协程管理器
            _coroutineManager = gameObject.AddComponent<CoroutineManager>();

            Debug.Log("协程多线程系统初始化完成");
        }

        /// <summary>
        /// 清理系统
        /// </summary>
        public void Cleanup()
        {
            _taskScheduler?.ClearAllTasks();
            ThreadPool.Instance?.Shutdown();
            
            Debug.Log("协程多线程系统清理完成");
        }

        /// <summary>
        /// 创建任务
        /// </summary>
        public ITask CreateTask(Action action, string taskId = null, TaskPriority priority = TaskPriority.Normal)
        {
            return _taskScheduler.CreateTask(action, priority, taskId);
        }

        /// <summary>
        /// 提交任务
        /// </summary>
        public void SubmitTask(ITask task)
        {
            _taskScheduler.SubmitTask(task);
        }

        /// <summary>
        /// 获取任务统计信息
        /// </summary>
        public TaskStatistics GetTaskStatistics()
        {
            return _taskScheduler.GetStatistics();
        }

        /// <summary>
        /// 任务调度器
        /// </summary>
        public TaskScheduler TaskScheduler => _taskScheduler;

        /// <summary>
        /// 等待指定时间后执行回调
        /// </summary>
        public void WaitForSeconds(float seconds, Action onComplete, object owner = null)
        {
            _coroutineManager?.WaitForSeconds(seconds, onComplete, owner);
        }

        /// <summary>
        /// 等待指定帧数后执行回调
        /// </summary>
        public void WaitForFrames(int frames, Action onComplete, object owner = null)
        {
            _coroutineManager?.WaitForFrames(frames, onComplete, owner);
        }

        /// <summary>
        /// 等待直到条件满足
        /// </summary>
        public void WaitUntil(Func<bool> condition, Action onComplete, object owner = null)
        {
            _coroutineManager?.WaitUntil(condition, onComplete, owner);
        }
    }
}
