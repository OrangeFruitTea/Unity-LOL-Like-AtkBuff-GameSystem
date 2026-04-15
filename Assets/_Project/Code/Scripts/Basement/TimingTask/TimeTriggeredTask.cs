using System;

namespace Basement.Tasks
{
    /// <summary>
    /// 时间触发任务
    /// 在指定时间或延迟后执行的任务
    /// </summary>
    public class TimeTriggeredTask : TimingTaskBase
    {
        private readonly Action _action;

        public TimeTriggeredTask(Action action, float delayTime, TimingTaskPriority priority = TimingTaskPriority.Normal)
            : base(null, priority, delayTime)
        {
            _action = action ?? throw new ArgumentNullException(nameof(action));
        }

        public TimeTriggeredTask(string taskId, Action action, float delayTime, TimingTaskPriority priority = TimingTaskPriority.Normal)
            : base(taskId, priority, delayTime)
        {
            _action = action ?? throw new ArgumentNullException(nameof(action));
        }

        /// <summary>
        /// 创建基于绝对时间的时间触发任务
        /// </summary>
        public static TimeTriggeredTask CreateScheduledTask(Action action, DateTime scheduledTime, TimingTaskPriority priority = TimingTaskPriority.Normal)
        {
            float delayTime = (float)(scheduledTime - DateTime.Now).TotalSeconds;
            return new TimeTriggeredTask(action, Math.Max(0, delayTime), priority);
        }

        protected override void ExecuteInternal()
        {
            _action?.Invoke();
        }

        public override string ToString()
        {
            return $"TimeTriggeredTask [TaskId: {TaskId}, Delay: {DelayTime}s, Priority: {Priority}]";
        }
    }
}
