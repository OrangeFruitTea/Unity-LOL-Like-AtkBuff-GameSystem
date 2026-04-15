using System;
using System.Collections.Generic;

namespace Basement.Threading
{
    /// <summary>
    /// 自定义线程池
    /// </summary>
    public class ThreadPool
    {
        private static volatile ThreadPool _instance;
        private static readonly object _lock = new object();

        private readonly List<WorkerThread> _threads = new List<WorkerThread>();
        private readonly ThreadSafeQueue<ITask> _taskQueue = new ThreadSafeQueue<ITask>();
        private bool _isRunning = false;

        public static ThreadPool Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new ThreadPool();
                        }
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// 初始化线程池
        /// </summary>
        public void Initialize(int minThreadCount = 2, int maxThreadCount = 8)
        {
            lock (_lock)
            {
                if (_isRunning)
                    return;

                _isRunning = true;
                
                // 创建线程
                for (int i = 0; i < minThreadCount; i++)
                {
                    CreateWorkerThread();
                }
            }
        }

        /// <summary>
        /// 创建工作线程
        /// </summary>
        private void CreateWorkerThread()
        {
            var thread = new WorkerThread(_taskQueue);
            _threads.Add(thread);
            thread.Start();
        }

        /// <summary>
        /// 提交任务
        /// </summary>
        public void SubmitTask(ITask task)
        {
            if (!_isRunning)
                throw new InvalidOperationException("ThreadPool is not running!");

            _taskQueue.Enqueue(task);
        }

        /// <summary>
        /// 停止线程池
        /// </summary>
        public void Shutdown()
        {
            lock (_lock)
            {
                if (!_isRunning)
                    return;

                _isRunning = false;
                
                // 停止所有线程
                foreach (var thread in _threads)
                {
                    thread.Stop();
                }
                
                _threads.Clear();
                _taskQueue.Clear();
            }
        }

        /// <summary>
        /// 获取当前任务数量
        /// </summary>
        public int TaskCount => _taskQueue.Count;

        /// <summary>
        /// 获取当前线程数量
        /// </summary>
        public int ThreadCount => _threads.Count;
    }
}
