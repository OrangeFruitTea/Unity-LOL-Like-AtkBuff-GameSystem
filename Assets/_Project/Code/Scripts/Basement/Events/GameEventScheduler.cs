using System;
using System.Collections.Generic;
using Basement.Utils;

namespace Basement.Events
{
    /// <summary>
    /// 事件调度器
    /// 负责事件的队列管理和调度
    /// </summary>
    public class GameEventScheduler : Singleton<GameEventScheduler>
    {
        private readonly PriorityQueue<IGameEvent> _eventQueue = new PriorityQueue<IGameEvent>();
        private readonly List<IGameEvent> _processingEvents = new List<IGameEvent>();
        private readonly object _lock = new object();
        private bool _isProcessing = false;
        private int _maxConcurrentEvents = 10;
        private float _processInterval = 0.016f; // 约60FPS
        private float _lastProcessTime = 0f;

        public void Initialize()
        {
        }

        /// <summary>
        /// 调度事件
        /// </summary>
        /// <param name="eventData">事件数据</param>
        public void Schedule(IGameEvent eventData)
        {
            if (eventData == null)
            {
                return;
            }

            lock (_lock)
            {
                _eventQueue.Enqueue(eventData, (int)eventData.Priority);
            }
        }

        /// <summary>
        /// 延迟调度事件
        /// </summary>
        /// <param name="eventData">事件数据</param>
        /// <param name="delay">延迟时间（秒）</param>
        public void ScheduleDelayed(IGameEvent eventData, float delay)
        {
            if (eventData == null)
            {
                return;
            }

            // 使用Unity协程实现延迟
            UnityEngine.MonoBehaviour monoBehaviour = UnityEngine.Object.FindObjectOfType<UnityEngine.MonoBehaviour>();
            if (monoBehaviour != null)
            {
                monoBehaviour.StartCoroutine(ScheduleDelayedCoroutine(eventData, delay));
            }
        }

        private System.Collections.IEnumerator ScheduleDelayedCoroutine(IGameEvent eventData, float delay)
        {
            yield return new UnityEngine.WaitForSeconds(delay);
            Schedule(eventData);
        }

        /// <summary>
        /// 处理事件队列
        /// </summary>
        public void ProcessQueue()
        {
            if (_isProcessing) return;

            lock (_lock)
            {
                if (_eventQueue.Count == 0) return;

                _isProcessing = true;
            }

            try
            {
                int processedCount = 0;

                while (processedCount < _maxConcurrentEvents)
                {
                    IGameEvent eventData;

                    lock (_lock)
                    {
                        if (_eventQueue.Count == 0) break;

                        eventData = _eventQueue.Dequeue();
                        _processingEvents.Add(eventData);
                    }

                    try
                    {
                        GameEventBus.Instance.Publish(eventData);
                        processedCount++;
                    }
                    catch (Exception ex)
                    {
                        // 处理异常
                    }
                    finally
                    {
                        lock (_lock)
                        {
                            _processingEvents.Remove(eventData);
                        }
                    }
                }
            }
            finally
            {
                _isProcessing = false;
            }
        }

        /// <summary>
        /// 更新调度器
        /// </summary>
        public void Update()
        {
            if (UnityEngine.Time.time - _lastProcessTime >= _processInterval)
            {
                ProcessQueue();
                _lastProcessTime = UnityEngine.Time.time;
            }
        }

        /// <summary>
        /// 清空事件队列
        /// </summary>
        public void ClearQueue()
        {
            lock (_lock)
            {
                _eventQueue.Clear();
                _processingEvents.Clear();
            }
        }

        /// <summary>
        /// 获取队列大小
        /// </summary>
        public int QueueSize
        {
            get
            {
                lock (_lock)
                {
                    return _eventQueue.Count;
                }
            }
        }

        /// <summary>
        /// 设置最大并发事件数
        /// </summary>
        public void SetMaxConcurrentEvents(int max)
        {
            _maxConcurrentEvents = Math.Max(1, max);
        }

        /// <summary>
        /// 设置处理间隔
        /// </summary>
        public void SetProcessInterval(float interval)
        {
            _processInterval = Math.Max(0.001f, interval);
        }
    }

    /// <summary>
    /// 优先级队列
    /// </summary>
    /// <typeparam name="T">元素类型</typeparam>
    public class PriorityQueue<T>
    {
        private readonly List<KeyValuePair<int, T>> _elements = new List<KeyValuePair<int, T>>();

        public int Count => _elements.Count;

        public void Enqueue(T item, int priority)
        {
            _elements.Add(new KeyValuePair<int, T>(priority, item));
            _elements.Sort((a, b) => b.Key.CompareTo(a.Key));
        }

        public T Dequeue()
        {
            if (_elements.Count == 0)
            {
                throw new InvalidOperationException("队列为空");
            }

            T item = _elements[0].Value;
            _elements.RemoveAt(0);
            return item;
        }

        public void Clear()
        {
            _elements.Clear();
        }
    }
}
