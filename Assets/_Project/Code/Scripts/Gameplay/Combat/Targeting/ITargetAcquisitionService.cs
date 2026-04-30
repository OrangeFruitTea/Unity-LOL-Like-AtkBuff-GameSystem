namespace Gameplay.Combat.Targeting
{
    /// <summary>无状态选敌；不写黑板、不写 <c>SkillCastContext</c>。见《MOBA普攻与瞬时伤害Impact投递》。</summary>
    public interface ITargetAcquisitionService
    {
        TargetAcquisitionResult Acquire(in TargetAcquisitionRequest request);
    }
}
