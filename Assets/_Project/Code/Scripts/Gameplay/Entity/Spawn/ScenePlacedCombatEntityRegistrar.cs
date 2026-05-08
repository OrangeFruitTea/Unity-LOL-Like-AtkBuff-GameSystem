using Core.ECS;
using UnityEngine;

namespace Core.Entity.Spawn
{
    /// <summary>
    /// 将<b>场景中直接摆放</b>的 <see cref="EntityBase"/>（未走 Instantiate + <see cref="EntitySpawnSystem.AddPendingEntity"/>）编入 ECS：<br/>
    /// 等价于为这些实例补一趟生成管线（Data + Profile + <see cref="IEntitySpawnExtension"/>）。可挂在关卡空物体或 Gameplay 根节点上。<br/>
    /// 已由 Spawner 入队或已完成绑定的实例会被 <see cref="EntitySpawnSystem.AddPendingEntity"/> 去重跳过。
    /// </summary>
    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    public sealed class ScenePlacedCombatEntityRegistrar : MonoBehaviour
    {
        [Tooltip("若为 true：入队后立即 Flush，便于同帧 Startup 读到 BoundEcsEntity / bridge。")]
        [SerializeField]
        private bool flushImmediately = true;

        [SerializeField]
        private bool includeInactiveEntityRoots;

        private void Start()
        {
            var world = EcsWorld.Instance;
            if (world == null)
            {
                Debug.LogError($"[{nameof(ScenePlacedCombatEntityRegistrar)}] EcsWorld.Instance 为空，无法注册场景实体。");
                return;
            }

            var spawnSystem = world.GetEcsSystem<EntitySpawnSystem>();
            if (spawnSystem == null)
            {
                Debug.LogError($"[{nameof(ScenePlacedCombatEntityRegistrar)}] EntitySpawnSystem 未注册。");
                return;
            }

#pragma warning disable CS0618 // FindObjectsOfType 单场景注册用；Unity 2023+ 仍可接受
            var roots = UnityEngine.Object.FindObjectsOfType<EntityBase>(includeInactiveEntityRoots);
#pragma warning restore CS0618

            var enqueued = 0;
            for (var i = 0; i < roots.Length; i++)
            {
                var eb = roots[i];
                if (eb == null || spawnSystem.HasPendingOrRegisteredBinding(eb))
                    continue;
                spawnSystem.AddPendingEntity(eb);
                enqueued++;
            }

            if (flushImmediately)
                spawnSystem.FlushPendingEntitiesNow();

            if (enqueued > 0)
                Debug.Log($"[{nameof(ScenePlacedCombatEntityRegistrar)}] 已为 {enqueued} 个场景摆放实体入网。");
        }
    }
}
