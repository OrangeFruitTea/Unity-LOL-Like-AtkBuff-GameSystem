namespace Gameplay.Presentation
{
    /// <summary>P1：仅供 UI（血条等）挂载；受击时由 <see cref="UnitAnimDrv"/> 回调，不做战斗逻辑。</summary>
    public interface IUnitHpBarFeedback
    {
        void OnHpDamagedShake();
    }
}
