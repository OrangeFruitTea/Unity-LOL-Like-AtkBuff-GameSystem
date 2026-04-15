using System;

namespace Basement.Tasks
{
    /// <summary>
    /// 周期性任务
    /// 按指定周期重复执行的任务
    /// </summary>
    public class PeriodicTask : TimingTaskBase
    {
        private readonly Action _action;
        private readonly int _repeatCount;
        private int _currentRepeat;

        public PeriodicTask(Action action, float interval, int repeatCount = -1, TimingTaskPriority priority = TimingTaskPriority.Normal)
            : base(null, priority, interval)
        {
            _action = action ?? throw new ArgumentNullException(nameof(action));
            _repeatCount = repeatCount;
            _currentRepeat = 0;
        }

        public PeriodicTask(string taskId, Action action, float interval, int repeatCount = -1, TimingTaskPriority priority = TimingTaskPriority.Normal)
            : base(taskId, priority, interval)
        {
            _action = action ?? throw new ArgumentNullException(nameof(action));
            _repeatCount = repeatCount;
            _currentRepeat = 0;
        }

        public override void Execute()
        {
            if (State != TimingTaskState.Ready)
            {
                return;
            }

            State = TimingTaskState.Running;

            try
            {
                _action?.Invoke();
                _currentRepeat++;

                if (_repeatCount > 0 && _currentRepeat >= _repeatCount)
                {
                    State = TimingTaskState.Completed;
                    _onCompleted?.Invoke();
                }
                else
                {
                    State = TimingTaskState.Ready;
                }
            }
            catch (Exception ex)
            {
                State = TimingTaskState.Completed;
                _onFailed?.Invoke(ex);
            }
        }

        protected override void ExecuteInternal()
        {
            _action?.Invoke();
        }

        public int CurrentRepeat => _currentRepeat;
        public int RepeatCount => _repeatCount;
        public float Interval => DelayTime;

        public override string ToString()
        {
            return $"PeriodicTask [TaskId: {TaskId}, Interval: {DelayTime}s, Repeat: {_currentRepeat}/{_repeatCount}]";
        }
    }
}
