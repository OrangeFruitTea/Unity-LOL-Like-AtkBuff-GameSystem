using Core.Entity;

namespace Gameplay.Combat.Targeting
{
    /// <summary>普攻出站：仅从 <see cref="Core.Entity.CombatBoardLiteComponent.AttackTargetEntityId"/> 解析目标后与塔 Strike 对齐。</summary>
    public interface ICombatImpactDispatch
    {
        bool TryDispatchNormalAttack(EntityBase attacker, out string error);
    }
}
