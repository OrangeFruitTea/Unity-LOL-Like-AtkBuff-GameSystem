using Core.ECS;
using UnityEngine;

namespace Basement.Runtime
{
    /// <summary>
    /// 在场景加载后初始化 Basement 单例并（可选）将泵送挂到 <see cref="EcsWorld"/>。
    /// </summary>
    public static class BasementRuntimeBootstrap
    {
        private static bool _pumpRegistered;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AfterSceneLoad()
        {
            if (!BasementRuntimeOptions.UseEcsWorldBasementLoop)
                return;

            if (_pumpRegistered)
                return;

            Basement.Tasks.TimingTaskManager.Instance.Initialize();
            Basement.Tasks.TimingTaskScheduler.Instance.Initialize();
            Basement.Events.GameEventBus.Instance.Initialize();
            Basement.Events.GameEventScheduler.Instance.Initialize();
            Basement.MatchTime.MatchTimeService.Instance.BeginMatch();

            var world = EcsWorld.Instance;
            if (world == null)
            {
                Debug.LogWarning("[BasementRuntimeBootstrap] EcsWorld 未就绪，跳过 BasementPumpEcsSystem 注册。");
                return;
            }

            world.AddEcsSystem(new BasementPumpEcsSystem());
            BasementLoopRouting.MarkEcsPumpActive();
            _pumpRegistered = true;
        }
    }
}
