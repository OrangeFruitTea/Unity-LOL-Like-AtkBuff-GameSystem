using Core.ECS;
using UnityEngine;

namespace Core.Entity
{
    /// <summary>
    /// 方案 A：不改变 <see cref="TowerEcsAttachments"/>，仅在水晶 Prefab 上额外挂此组件。<br/>
    /// 生成链中 <see cref="IEntitySpawnExtension.OnAfterEcsBaseSpawned"/> 会为实体追加 <see cref="CrystalCoreObjectiveComponent"/>。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CrystalObjectiveEcsAttachment : MonoBehaviour, IEntitySpawnExtension
    {
        [SerializeField]
        private FactionTeamId owningTeam = FactionTeamId.Blue;

        /// <inheritdoc />
        public void OnAfterEcsBaseSpawned(EcsEntity ecs, EntityBase host)
        {
            if (owningTeam == FactionTeamId.Neutral)
            {
                Debug.LogWarning($"{nameof(CrystalObjectiveEcsAttachment)} on '{name}': crystal cannot use Neutral — component not added.");
                return;
            }

            var crystal = default(CrystalCoreObjectiveComponent);
            crystal.InitializeDefaults();
            crystal.OwningTeamId = (byte)owningTeam;
            EcsWorld.AddComponent(ecs, crystal);
        }
    }
}
