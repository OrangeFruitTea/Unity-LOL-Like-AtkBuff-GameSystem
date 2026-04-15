using System;
using System.Collections.Generic;

namespace Basement.Tasks
{
    /// <summary>
    /// 批量任务处理器
    /// 用于批量处理任务，提高处理效率
    /// </summary>
    public class BatchTaskProcessor
    {
        private readonly List<ITimingTask> _batch = new List<ITimingTask>();
        private int _batchSize = 50;
        private float _batchInterval = 0.05f;
        private float _lastBatchTime = 0f;

        /// <summary>
        /// 添加任务到批处理
        /// </summary>
        public void AddTask(ITimingTask task)
        {
            _batch.Add(task);

            if (_batch.Count >= _batchSize ||
                UnityEngine.Time.time - _lastBatchTime >= _batchInterval)
            {
                ProcessBatch();
            }
        }

        /// <summary>
        /// 处理批任务
        /// </summary>
        private void ProcessBatch()
        {
            if (_batch.Count == 0) return;

            foreach (var task in _batch)
            {
                task.Execute();
            }

            _batch.Clear();
            _lastBatchTime = UnityEngine.Time.time;
        }

        /// <summary>
        /// 强制处理所有任务
        /// </summary>
        public void ForceProcess()
        {
            ProcessBatch();
        }

        /// <summary>
        /// 当前批处理中的任务数量
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
