using System.Collections.Generic;

namespace Basement.Events
{
    /// <summary>
    /// 事件池
    /// 用于事件对象的池化管理，减少GC压力
    /// </summary>
    /// <typeparam name="T">事件类型</typeparam>
    public class EventPool<T> where T : IGameEvent, new()
    {
        private readonly Stack<T> _pool = new Stack<T>();
        private readonly object _lock = new object();

        /// <summary>
        /// 从池中获取事件
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
        /// 将事件返回池中
        /// </summary>
        public void Return(T eventData)
        {
            if (eventData == null) return;

            lock (_lock)
            {
                _pool.Push(eventData);
            }
        }

        /// <summary>
        /// 池中事件数量
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
