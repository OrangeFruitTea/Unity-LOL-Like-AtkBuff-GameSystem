using UnityEngine;
using Basement.Tasks;

namespace Basement.Tasks.Unity
{
    /// <summary>
    /// 任务调度器Unity集成
    /// 用于在Unity环境中管理任务调度
    /// </summary>
    public class TaskDispatcher : MonoBehaviour
    {
        private void Awake()
        {
            // 初始化任务管理器和调度器
            TimingTaskManager.Instance.Initialize();
            TimingTaskScheduler.Instance.Initialize();
        }

        private void Update()
        {
            // 更新任务调度器
            TimingTaskScheduler.Instance.Update();
        }

        private void OnDestroy()
        {
            // 清理所有任务
            TimingTaskManager.Instance.ClearAllTasks();
        }

        /// <summary>
        /// 静态方法：创建任务调度器实例
        /// </summary>
        public static TaskDispatcher Create()
        {
            GameObject go = new GameObject("TaskDispatcher");
            return go.AddComponent<TaskDispatcher>();
        }

        /// <summary>
        /// 静态方法：获取或创建任务调度器实例
        /// </summary>
        public static TaskDispatcher GetOrCreate()
        {
            TaskDispatcher dispatcher = FindObjectOfType<TaskDispatcher>();
            if (dispatcher == null)
            {
                dispatcher = Create();
            }
            return dispatcher;
        }
    }
}
