using System;

namespace Basement.Tasks
{
    /// <summary>
    /// 任务基类
    /// 提供任务的基本实现
    /// </summary>
    public abstract class TimingTaskBase : ITimingTask
    {
        public string TaskId { get; protected set; }
        public TimingTaskState State { get; protected set; }
        public TimingTaskPriority Priority { get; protected set; }
        public float DelayTime { get; protected set; }

        protected Action _onCompleted;
        protected Action _onCancelled;
        protected Action<Exception> _onFailed;

        protected TimingTaskBase(string taskId, TimingTaskPriority priority, float delayTime)
        {
            TaskId = taskId ?? Guid.NewGuid().ToString();
            Priority = priority;
            State = TimingTaskState.Ready;
            DelayTime = delayTime;
        }

        public virtual void Execute()
        {
            if (State != TimingTaskState.Ready)
            {
                return;
            }

            State = TimingTaskState.Running;

            try
            {
                ExecuteInternal();
                State = TimingTaskState.Completed;
                _onCompleted?.Invoke();
            }
            catch (Exception ex)
            {
                State = TimingTaskState.Completed;
                _onFailed?.Invoke(ex);
            }
        }

        public virtual void Cancel()
        {
            if (State == TimingTaskState.Completed)
            {
                return;
            }

            State = TimingTaskState.Completed;
            _onCancelled?.Invoke();
        }

        protected abstract void ExecuteInternal();

        public TimingTaskBase OnCompleted(Action callback)
        {
            _onCompleted += callback;
            return this;
        }

        public TimingTaskBase OnCancelled(Action callback)
        {
            _onCancelled += callback;
            return this;
        }

        public TimingTaskBase OnFailed(Action<Exception> callback)
        {
            _onFailed += callback;
            return this;
        }
    }
}
