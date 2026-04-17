using UnityEngine;
using Basement.MatchTime;
using Basement.Runtime;
using Basement.Tasks;

namespace Basement.Tasks.Unity
{
    /// <summary>
    /// 任务调度器 Unity 集成。若启用 <see cref="BasementRuntimeOptions.UseEcsWorldBasementLoop"/> 且已成功注册
    /// <see cref="BasementPumpEcsSystem"/>，则本组件不再执行泵送，仅保留初始化/销毁时的生命周期（可与场景共存）。
    /// </summary>
    public class TaskDispatcher : MonoBehaviour
    {
        private void Awake()
        {
            // 初始化任务管理器和调度器
            TimingTaskManager.Instance.Initialize();
            TimingTaskScheduler.Instance.Initialize();
            // 未显式编排对局流程时，默认可推进定时任务；正式对局可在 Enter/Exit 中改调 EndMatch/BeginMatch
            MatchTimeService.Instance.BeginMatch();
        }

        private void Update()
        {
            if (BasementLoopRouting.IsEcsPumpActive)
                return;
            MatchTimeService.Instance.TickUpdate();
            TimingTaskScheduler.Instance.Update();
        }

        private void FixedUpdate()
        {
            if (BasementLoopRouting.IsEcsPumpActive)
                return;
            MatchTimeService.Instance.TickFixedUpdate();
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
