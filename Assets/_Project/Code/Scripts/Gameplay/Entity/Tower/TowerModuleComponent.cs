using Core.ECS;

namespace Core.Entity
{
    /// <summary> 静态/表侧防御塔字段；节拍状态见 <see cref="TowerCombatCycleComponent"/>。§5 design doc。 </summary>
    public struct TowerModuleComponent : IEcsComponent
    {
        public int LaneSlotId;

        public float AggroAcquireRange;

        public TowerTargetingMode TargetingMode;

        public ushort PlatingStacks;

        public bool AggroHeroHint;

        public float WarningDuration;

        public float LockDuration;

        /// <summary> 锁定结束后到 Impact 落地的额外延迟（秒）。 </summary>
        public float StrikeHitDelay;

        public float AttackCooldown;

        public void InitializeDefaults()
        {
            LaneSlotId = 0;
            AggroAcquireRange = 8f;
            TargetingMode = TowerTargetingMode.NearestThreat;
            PlatingStacks = 0;
            AggroHeroHint = false;
            WarningDuration = 0.25f;
            LockDuration = 0.15f;
            StrikeHitDelay = 0f;
            AttackCooldown = 1.2f;
        }
    }
}
