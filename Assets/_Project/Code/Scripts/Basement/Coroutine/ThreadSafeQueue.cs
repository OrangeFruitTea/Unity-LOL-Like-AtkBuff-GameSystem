using System;
using System.Collections.Generic;
using System.Threading;

namespace Basement.Threading
{
    /// <summary>
    /// 线程安全队列
    /// </summary>
    public class ThreadSafeQueue<T>
    {
        private readonly Queue<T> _queue = new Queue<T>();
        private readonly object _lock = new object();
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(0);

        /// <summary>
        /// 入队
        /// </summary>
        public void Enqueue(T item)
        {
            lock (_lock)
            {
                _queue.Enqueue(item);
            }
            _semaphore.Release();
        }

        /// <summary>
        /// 出队
        /// </summary>
        public bool TryDequeue(out T item)
        {
            if (_semaphore.Wait(0))
            {
                lock (_lock)
                {
                    item = _queue.Dequeue();
                    return true;
                }
            }
            item = default;
            return false;
        }

        /// <summary>
        /// 阻塞出队
        /// </summary>
        public T Dequeue()
        {
            _semaphore.Wait();
            lock (_lock)
            {
                return _queue.Dequeue();
            }
        }

        /// <summary>
        /// 尝试阻塞出队
        /// </summary>
        public bool TryDequeue(out T item, int timeoutMilliseconds)
        {
            if (_semaphore.Wait(timeoutMilliseconds))
            {
                lock (_lock)
                {
                    item = _queue.Dequeue();
                    return true;
                }
            }
            item = default;
            return false;
        }

        /// <summary>
        /// 获取队列长度
        /// </summary>
        public int Count
        {
            get
            {
                lock (_lock)
                {
                    return _queue.Count;
                }
            }
        }

        /// <summary>
        /// 清空队列
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                _queue.Clear();
            }
        }
    }
}
