using System;
using System.Collections.Generic;

namespace Basement.Events
{
    /// <summary>
    /// 批量事件处理器
    /// 用于批量处理事件，提高处理效率
    /// </summary>
    public class BatchEventProcessor
    {
        private readonly List<IGameEvent> _batch = new List<IGameEvent>();
        private int _batchSize = 100;
        private float _batchInterval = 0.1f;
        private float _lastBatchTime = 0f;

        /// <summary>
        /// 添加事件到批处理
        /// </summary>
        public void AddEvent(IGameEvent eventData)
        {
            _batch.Add(eventData);

            if (_batch.Count >= _batchSize ||
                UnityEngine.Time.time - _lastBatchTime >= _batchInterval)
            {
                ProcessBatch();
            }
        }

        /// <summary>
        /// 处理批事件
        /// </summary>
        private void ProcessBatch()
        {
            if (_batch.Count == 0) return;

            foreach (var eventData in _batch)
            {
                GameEventBus.Instance.Publish(eventData);
            }

            _batch.Clear();
            _lastBatchTime = UnityEngine.Time.time;
        }

        /// <summary>
        /// 强制处理所有事件
        /// </summary>
        public void ForceProcess()
        {
            ProcessBatch();
        }

        /// <summary>
        /// 当前批处理中的事件数量
        /// </summary>
        public int BatchCount => _batch.Count;

        /// <summary>
        /// 设置批处理大小
        /// </summary>
        public void SetBatchSize(int size)
        {
            _batchSize = Math.Max(1, size);
        }

        /// <summary>
        /// 设置批处理间隔
        /// </summary>
        public void SetBatchInterval(float interval)
        {
            _batchInterval = Math.Max(0.001f, interval);
        }
    }
}
