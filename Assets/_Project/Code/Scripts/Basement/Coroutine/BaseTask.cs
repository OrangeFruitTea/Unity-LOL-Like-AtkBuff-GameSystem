using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Basement.Threading
{
    /// <summary>
    /// 基础任务实现类
    /// </summary>
    public abstract class BaseTask : ITask
    {
        private readonly List<ITask> _dependencies = new List<ITask>();
        private readonly List<ITask> _dependents = new List<ITask>();
        private TaskStatus _status = TaskStatus.Created;
        private Exception _exception = null;
        private readonly object _lock = new object();

        /// <summary>
        /// 任务唯一标识符
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// 任务优先级
        /// </summary>
        public TaskPriority Priority { get; set; }

        /// <summary>
        /// 任务状态
        /// </summary>
        public TaskStatus Status
        {
            get { return _status; }
            set
            {
                lock (_lock)
                {
                    if (_status != value)
                    {
                        TaskStatus oldStatus = _status;
                        _status = value;
                        OnStatusChanged?.Invoke(this, oldStatus, _status);
                    }
                }
            }
        }

        /// <summary>
        /// 任务依赖的前置任务
        /// </summary>
        public IReadOnlyCollection<ITask> Dependencies
        {
            get { return _dependencies.AsReadOnly(); }
        }

        /// <summary>
        /// 依赖此任务的后继任务
        /// </summary>
        public IReadOnlyCollection<ITask> Dependents
        {
            get { return _dependents.AsReadOnly(); }
        }

        /// <summary>
        /// 任务执行异常（如果有）
        /// </summary>
        public Exception Exception
        {
            get { return _exception; }
            protected set { _exception = value; }
        }

        /// <summary>
        /// 任务完成回调
        /// </summary>
        public Action OnComplete { get; set; }

        /// <summary>
        /// 任务状态变化回调
        /// </summary>
        public Action<ITask, TaskStatus, TaskStatus> OnStatusChanged { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        protected BaseTask(string id = null)
        {
            Id = id ?? Guid.NewGuid().ToString();
            Priority = TaskPriority.Normal;
        }

        /// <summary>
        /// 执行任务
        /// </summary>
        public abstract void Execute();

        /// <summary>
        /// 添加依赖任务
        /// </summary>
        /// <param name="dependency">依赖的任务</param>
        /// <exception cref="ArgumentNullException">当dependency为null时抛出</exception>
        /// <exception cref="ArgumentException">当尝试添加自身作为依赖时抛出</exception>
        public void AddDependency(ITask dependency)
        {
            if (dependency == null)
                throw new ArgumentNullException(nameof(dependency), "依赖任务不能为null");

            if (dependency == this)
                throw new ArgumentException("不能将自身添加为依赖", nameof(dependency));

            lock (_lock)
            {
                if (_dependencies.Contains(dependency))
                    return;

                _dependencies.Add(dependency);
                
                if (dependency is BaseTask baseTask)
                {
                    lock (baseTask._lock)
                    {
                        if (!baseTask._dependents.Contains(this))
                        {
                            baseTask._dependents.Add(this);
                        }
                    }
                }
                
                UpdateStatus();
            }
        }

        /// <summary>
        /// 移除依赖任务
        /// </summary>
        /// <param name="dependency">要移除的依赖任务</param>
        /// <returns>是否成功移除</returns>
        public bool RemoveDependency(ITask dependency)
        {
            if (dependency == null)
                return false;

            lock (_lock)
            {
                if (!_dependencies.Contains(dependency))
                    return false;

                _dependencies.Remove(dependency);
                
                if (dependency is BaseTask baseTask)
                {
                    lock (baseTask._lock)
                    {
                        baseTask._dependents.Remove(this);
                    }
                }
                
                UpdateStatus();
                return true;
            }
        }

        /// <summary>
        /// 更新任务状态
        /// 根据依赖任务的状态计算当前任务的状态
        /// </summary>
        public void UpdateStatus()
        {
            lock (_lock)
            {
                if (_status == TaskStatus.Running || _status == TaskStatus.Completed || 
                    _status == TaskStatus.Failed || _status == TaskStatus.Canceled)
                    return;

                bool allDependenciesCompleted = _dependencies.Count == 0 || 
                    _dependencies.All(dep => dep.Status == TaskStatus.Completed);
                
                Status = allDependenciesCompleted ? TaskStatus.Ready : TaskStatus.Waiting;
            }
        }

        /// <summary>
        /// 内部完成回调
        /// 通知所有后继任务更新状态
        /// </summary>
        protected void OnCompleteInternal()
        {
            lock (_lock)
            {
                foreach (var dependent in _dependents)
                {
                    dependent.UpdateStatus();
                }
            }
        }

        /// <summary>
        /// 比较两个任务是否相等
        /// </summary>
        /// <param name="obj">要比较的对象</param>
        /// <returns>是否相等</returns>
        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj is ITask task) return Id == task.Id;
            return false;
        }

        /// <summary>
        /// 获取哈希码
        /// </summary>
        /// <returns>哈希码</returns>
        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        /// <summary>
        /// 重载相等运算符
        /// </summary>
        /// <param name="left">左侧操作数</param>
        /// <param name="right">右侧操作数</param>
        /// <returns>是否相等</returns>
        public static bool operator ==(BaseTask left, object right)
        {
            if (left is null) return right is null;
            return left.Equals(right);
        }

        /// <summary>
        /// 重载不等运算符
        /// </summary>
        /// <param name="left">左侧操作数</param>
        /// <param name="right">右侧操作数</param>
        /// <returns>是否不等</returns>
        public static bool operator !=(BaseTask left, object right)
        {
            return !(left == right);
        }
    }
}