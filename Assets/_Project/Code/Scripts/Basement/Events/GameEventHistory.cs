using System;
using System.Collections.Generic;
using Basement.Utils;

namespace Basement.Events
{
    /// <summary>
    /// 事件历史记录器
    /// 负责记录事件的历史信息
    /// </summary>
    public class GameEventHistory : Singleton<GameEventHistory>
    {
        private readonly Queue<GameEventRecord> _eventRecords = new Queue<GameEventRecord>();
        private readonly object _lock = new object();
        private int _maxRecords = 1000;
        private bool _isEnabled = false;

        public void Initialize(bool enabled = false, int maxRecords = 1000)
        {
            _maxRecords = maxRecords;
            _isEnabled = enabled;
        }

        /// <summary>
        /// 记录事件
        /// </summary>
        /// <param name="eventData">事件数据</param>
        public void RecordEvent(IGameEvent eventData)
        {
            if (!_isEnabled || eventData == null) return;

            lock (_lock)
            {
                GameEventRecord record = new GameEventRecord
                {
                    EventId = eventData.EventId,
                    EventType = eventData.GetType().Name,
                    Timestamp = eventData.Timestamp,
                    Priority = eventData.Priority
                };

                _eventRecords.Enqueue(record);

                // 超过最大记录数时移除旧记录
                while (_eventRecords.Count > _maxRecords)
                {
                    _eventRecords.Dequeue();
                }
            }
        }

        /// <summary>
        /// 获取事件记录
        /// </summary>
        /// <param name="count">记录数量</param>
        /// <returns>事件记录列表</returns>
        public List<GameEventRecord> GetRecords(int count = 10)
        {
            lock (_lock)
            {
                List<GameEventRecord> records = new List<GameEventRecord>();

                foreach (var record in _eventRecords)
                {
                    records.Add(record);
                    if (records.Count >= count) break;
                }

                return records;
            }
        }

        /// <summary>
        /// 清空记录
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                _eventRecords.Clear();
            }
        }

        /// <summary>
        /// 启用记录
        /// </summary>
        public void Enable()
        {
            _isEnabled = true;
        }

        /// <summary>
        /// 禁用记录
        /// </summary>
        public void Disable()
        {
            _isEnabled = false;
        }
    }

    /// <summary>
    /// 事件记录
    /// </summary>
    public class GameEventRecord
    {
        public string EventId { get; set; }
        public string EventType { get; set; }
        public DateTime Timestamp { get; set; }
        public GameEventPriority Priority { get; set; }
    }
}
