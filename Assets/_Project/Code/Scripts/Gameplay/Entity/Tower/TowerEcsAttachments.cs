using Core.ECS;
using UnityEngine;

namespace Core.Entity
{
    /// <summary>
    /// 塔 Prefab：在 ECS 注册后附加 <see cref="TowerModuleComponent"/> 与 <see cref="TowerCombatCycleComponent"/>。
    /// 需同实体已挂 <see cref="CombatEntitySpawnProfile"/>（阵营、黑板、<see cref="UnitArchetype.Tower"/>）。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TowerEcsAttachments : MonoBehaviour, IEntitySpawnExtension
    {
        [SerializeField] private TowerModulePreset module = TowerModulePreset.CreateDefault();

        public void OnAfterEcsBaseSpawned(EcsEntity ecs, EntityBase host)
        {
            var towerMod = module.ToRuntime();
            EcsWorld.AddComponent(ecs, towerMod);

            var cycle = new TowerCombatCycleComponent();
            cycle.InitializeDefaults();
            EcsWorld.AddComponent(ecs, cycle);
        }

        [System.Serializable]
        public struct TowerModulePreset
        {
            public int LaneSlotId;
            public float AggroAcquireRange;
            public TowerTargetingMode TargetingMode;
            public ushort PlatingStacks;
            public bool AggroHeroHint;
            public float WarningDuration;
            public float LockDuration;
            public float StrikeHitDelay;
            public float AttackCooldown;

            public static TowerModulePreset CreateDefault()
            {
                var m = default(TowerModulePreset);
                var tmp = new TowerModuleComponent();
                tmp.InitializeDefaults();
                m.LaneSlotId = tmp.LaneSlotId;
                m.AggroAcquireRange = tmp.AggroAcquireRange;
                m.TargetingMode = tmp.TargetingMode;
                m.PlatingStacks = tmp.PlatingStacks;
                m.AggroHeroHint = tmp.AggroHeroHint;
                m.WarningDuration = tmp.WarningDuration;
                m.LockDuration = tmp.LockDuration;
                m.StrikeHitDelay = tmp.StrikeHitDelay;
                m.AttackCooldown = tmp.AttackCooldown;
                return m;
            }

            public TowerModuleComponent ToRuntime()
            {
                return new TowerModuleComponent
                {
                    LaneSlotId = LaneSlotId,
                    AggroAcquireRange = AggroAcquireRange,
                    TargetingMode = TargetingMode,
                    PlatingStacks = PlatingStacks,
                    AggroHeroHint = AggroHeroHint,
                    WarningDuration = WarningDuration,
                    LockDuration = LockDuration,
                    StrikeHitDelay = StrikeHitDelay,
                    AttackCooldown = AttackCooldown,
                };
            }
        }
    }
}
