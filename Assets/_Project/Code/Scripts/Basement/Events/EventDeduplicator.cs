using System;
using System.Collections.Generic;

namespace Basement.Events
{
    /// <summary>
    /// 事件去重器
    /// 用于过滤重复事件，提高系统效率
    /// </summary>
    public class EventDeduplicator
    {
        private readonly HashSet<string> _recentEventIds = new HashSet<string>();
        private readonly Queue<string> _eventQueue = new Queue<string>();
        private int _maxRecentEvents = 1000;

        /// <summary>
        /// 检查事件是否应该被处理（去重）
        /// </summary>
        /// <param name="eventData">事件数据</param>
        /// <returns>是否应该处理该事件</returns>
        public bool ShouldProcessEvent(IGameEvent eventData)
        {
            if (eventData == null) return false;

            string eventKey = $"{eventData.EventId}_{eventData.Timestamp.Ticks}";

            if (_recentEventIds.Contains(eventKey))
            {
                return false;
            }

            _recentEventIds.Add(eventKey);
            _eventQueue.Enqueue(eventKey);

            // 限制最近事件数量
            while (_eventQueue.Count > _maxRecentEvents)
            {
                string oldKey = _eventQueue.Dequeue();
                _recentEventIds.Remove(oldKey);
            }

            return true;
        }

        /// <summary>
        /// 清空去重记录
        /// </summary>
        public void Clear()
        {
            _recentEventIds.Clear();
            _eventQueue.Clear();
        }

        /// <summary>
        /// 设置最大最近事件数量
        /// </summary>
        public void SetMaxRecentEvents(int max)
        {
            _maxRecentEvents = Math.Max(1, max);
        }
    }
}
