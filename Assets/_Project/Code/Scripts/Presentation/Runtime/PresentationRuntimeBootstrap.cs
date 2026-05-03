using Core.ECS;
using Core.UI;
using UnityEngine;

namespace Presentation.Runtime
{
    /// <summary>
    /// 表现层运行时引导：在 <see cref="RuntimeInitializeLoadType.BeforeSceneLoad"/> 确保 <see cref="EcsWorld"/> 与 <see cref="UIManager"/> 就绪。
    /// DetailStatement 等需在单位 <see cref="EcsEntityBridge"/> 有效后由生成方（如 <see cref="Core.Entity.TestPlayerSpawner"/>）再 <c>TrySpawnDetailStatement</c> + <c>BindEcsBridgeConsumers</c>。
    /// </summary>
    public static class PresentationRuntimeBootstrap
    {
        private static bool _warmed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void BeforeSceneLoad()
        {
            if (_warmed)
                return;
            _warmed = true;

            _ = EcsWorld.Instance;

            var ui = UIManager.Instance;
            if (ui == null)
                Debug.LogWarning("[PresentationRuntimeBootstrap] UIManager.Instance 为空，跳过 UI 预热。");
        }
    }
}
