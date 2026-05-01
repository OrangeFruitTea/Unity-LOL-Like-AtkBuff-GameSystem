using Core.Entity;
using Core.ECS;

namespace Gameplay.Presentation
{
    /// <summary>由 Impact 等逻辑层单向调用 → 宿主 <see cref="UnitAnimDrv"/> 播受击与 UI。</summary>
    public static class HitFxRelay
    {
        public static void RaiseHpDamaged(EcsEntity target, float magnitude)
        {
            if (!target.IsValid() || magnitude <= 0f)
                return;

            if (!EntityEcsLinkRegistry.TryGetEntityBase(target, out var host))
                return;

            host.GetComponent<UnitAnimDrv>()?.NotifyDamaged();
        }
    }
}
