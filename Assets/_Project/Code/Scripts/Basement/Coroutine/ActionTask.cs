using System;

namespace Basement.Threading
{
    /// <summary>
    /// Action任务实现
    /// </summary>
    public class ActionTask : BaseTask
    {
        private readonly Action _action;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="action">要执行的Action</param>
        /// <param name="priority">任务优先级</param>
        /// <param name="id">任务唯一标识符</param>
        public ActionTask(Action action, TaskPriority priority = TaskPriority.Normal, string id = null)
            : base(id)
        {
            _action = action;
            Priority = priority;
        }

        /// <summary>
        /// 执行任务
        /// </summary>
        public override void Execute()
        {
            try
            {
                Status = TaskStatus.Running;
                _action?.Invoke();
                Status = TaskStatus.Completed;
            }
            catch (Exception ex)
            {
                Exception = ex;
                Status = TaskStatus.Failed;
                throw;
            }
            finally
            {
                OnComplete?.Invoke();
                OnCompleteInternal();
            }
        }
    }
}