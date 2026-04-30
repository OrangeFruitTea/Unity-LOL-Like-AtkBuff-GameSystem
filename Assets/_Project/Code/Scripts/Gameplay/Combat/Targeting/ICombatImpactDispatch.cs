using Core.Entity;

namespace Gameplay.Combat.Targeting
{
    /// <summary>MVP 普攻出站：内部 <c>CreateImpactEvent</c>，与塔 Strike 对齐。</summary>
    public interface ICombatImpactDispatch
    {
        bool TryDispatchNormalAttack(EntityBase attacker, EntityBase victim, out string error);
    }
}
