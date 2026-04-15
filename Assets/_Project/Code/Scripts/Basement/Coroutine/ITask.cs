using System;
using System.Collections.Generic;

namespace Basement.Threading
{
    /// <summary>
    /// 任务接口
    /// </summary>
    public interface ITask
    {
        /// <summary>
        /// 任务唯一标识符
        /// </summary>
        string Id { get; }
        
        /// <summary>
        /// 任务优先级
        /// </summary>
        TaskPriority Priority { get; set; }
        
        /// <summary>
        /// 任务状态
        /// </summary>
        TaskStatus Status { get; set; }
        
        /// <summary>
        /// 任务依赖的前置任务
        /// </summary>
        IReadOnlyCollection<ITask> Dependencies { get; }
        
        /// <summary>
        /// 依赖此任务的后继任务
        /// </summary>
        IReadOnlyCollection<ITask> Dependents { get; }
        
        /// <summary>
        /// 任务执行异常（如果有）
        /// </summary>
        Exception Exception { get; }
        
        /// <summary>
        /// 执行任务
        /// </summary>
        void Execute();
        
        /// <summary>
        /// 任务完成回调
        /// </summary>
        Action OnComplete { get; set; }
        
        /// <summary>
        /// 添加依赖任务
        /// </summary>
        void AddDependency(ITask dependency);
        
        /// <summary>
        /// 移除依赖任务
        /// </summary>
        bool RemoveDependency(ITask dependency);
        
        /// <summary>
        /// 更新任务状态
        /// </summary>
        void UpdateStatus();
        
        /// <summary>
        /// 任务状态变化回调
        /// </summary>
        Action<ITask, TaskStatus, TaskStatus> OnStatusChanged { get; set; }
    }
}
