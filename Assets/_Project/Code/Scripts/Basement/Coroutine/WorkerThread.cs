using System;
using System.Threading;

namespace Basement.Threading
{
    /// <summary>
    /// 工作线程
    /// </summary>
    internal class WorkerThread
    {
        private readonly ThreadSafeQueue<ITask> _taskQueue;
        private Thread _thread;
        private bool _isRunning = false;

        public WorkerThread(ThreadSafeQueue<ITask> taskQueue)
        {
            _taskQueue = taskQueue;
        }

        public void Start()
        {
            _isRunning = true;
            _thread = new Thread(Run) { IsBackground = true, Name = "WorkerThread" };
            _thread.Start();
        }

        public void Stop()
        {
            _isRunning = false;
        }

        private void Run()
        {
            while (_isRunning)
            {
                try
                {
                    if (_taskQueue.TryDequeue(out ITask task, 100))
                    {
                        task.Execute();
                        task.OnComplete?.Invoke();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"WorkerThread exception: {ex.Message}");
                }
            }
        }
    }
}
