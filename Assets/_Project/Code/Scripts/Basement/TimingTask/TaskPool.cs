using System.Collections.Generic;

namespace Basement.Tasks
{
    /// <summary>
    /// 任务池
    /// 用于任务对象的池化管理，减少GC压力
    /// </summary>
    /// <typeparam name="T">任务类型</typeparam>
    public class TaskPool<T> where T : ITimingTask, new()
    {
        private readonly Stack<T> _pool = new Stack<T>();
        private readonly object _lock = new object();

        /// <summary>
        /// 从池中获取任务
        /// </summary>
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

        /// <summary>
        /// 将任务返回池中
        /// </summary>
        public void Return(T task)
        {
            if (task == null) return;

            lock (_lock)
            {
                _pool.Push(task);
            }
        }

        /// <summary>
        /// 池中任务数量
        /// </summary>
        public int Count
        {
            get
            {
                lock (_lock)
                {
                    return _pool.Count;
                }
            }
        }

        /// <summary>
        /// 清空池
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                _pool.Clear();
            }
        }
    }
}
