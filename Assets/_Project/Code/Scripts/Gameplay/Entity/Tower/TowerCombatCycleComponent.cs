using Core.ECS;

namespace Core.Entity
{
    /// <summary> 塔普攻 **时间轴**；目标 id 不写在此，见 <see cref="CombatBoardLiteComponent"/>。§5 design doc。 </summary>
    public struct TowerCombatCycleComponent : IEcsComponent
    {
        public TowerCombatPhase Phase;

        /// <summary> <see cref="UnityEngine.Time.time"/> 比较：当前相位于何时结束。 </summary>
        public float PhaseEndsAt;

        /// <summary> Strike 帧排程；无效时可置 < 0。 </summary>
        public float PendingImpactAt;

        public void InitializeDefaults()
        {
            Phase = TowerCombatPhase.IdleScan;
            PhaseEndsAt = 0f;
            PendingImpactAt = -1f;
        }
    }
}
