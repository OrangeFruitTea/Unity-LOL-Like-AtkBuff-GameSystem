using Core.Entity;

namespace Gameplay.Presentation
{
    /// <summary>
    /// 受击/扣血时在<strong>宿主子树</strong>上进行表现回调（典型实现：血条抖动）。<br/>
    /// UI 脚本挂在同一 <see cref="EntityBase"/> 层级或其子物体上。
    /// </summary>
    public readonly struct UnitDamagePresentationPayload
    {
        public readonly EntityBase VictimEntity;
        public readonly float DamageAmountHp;

        public UnitDamagePresentationPayload(EntityBase victimEntity, float damageAmountHp)
        {
            VictimEntity = victimEntity;
            DamageAmountHp = damageAmountHp;
        }
    }

    public interface IUnitDamagedPresentation
    {
        /// <summary>逻辑层已对 HP 做结算后调用（主线程）；勿在此写 ECS 血量。</summary>
        void OnUnitDamaged(in UnitDamagePresentationPayload payload);
    }
}
