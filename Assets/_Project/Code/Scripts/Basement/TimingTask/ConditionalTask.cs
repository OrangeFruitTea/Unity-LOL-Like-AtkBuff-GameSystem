using System;

namespace Basement.Tasks
{
    /// <summary>
    /// 条件任务
    /// 当满足指定条件时执行的任务
    /// </summary>
    public class ConditionalTask : TimingTaskBase
    {
        private readonly Action _action;
        private readonly Func<bool> _condition;

        public ConditionalTask(Action action, Func<bool> condition, float checkInterval = 0.1f, TimingTaskPriority priority = TimingTaskPriority.Normal)
            : base(null, priority, checkInterval)
        {
            _action = action ?? throw new ArgumentNullException(nameof(action));
            _condition = condition ?? throw new ArgumentNullException(nameof(condition));
        }

        public ConditionalTask(string taskId, Action action, Func<bool> condition, float checkInterval = 0.1f, TimingTaskPriority priority = TimingTaskPriority.Normal)
            : base(taskId, priority, checkInterval)
        {
            _action = action ?? throw new ArgumentNullException(nameof(action));
            _condition = condition ?? throw new ArgumentNullException(nameof(condition));
        }

        public override void Execute()
        {
            if (State != TimingTaskState.Ready)
            {
                return;
            }

            try
            {
                if (_condition())
                {
                    State = TimingTaskState.Running;
                    _action?.Invoke();
                    State = TimingTaskState.Completed;
                    _onCompleted?.Invoke();
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

        public float CheckInterval => DelayTime;

        public override string ToString()
        {
            return $"ConditionalTask [TaskId: {TaskId}, CheckInterval: {DelayTime}s]";
        }
    }
}
