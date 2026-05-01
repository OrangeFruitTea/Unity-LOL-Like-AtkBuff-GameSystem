using System.Collections.Generic;
using Core.ECS;
using Gameplay.Entity;

namespace Core.Entity
{
    /// <summary>
    /// 统一生与死门槛；击倒时写入 <see cref="CombatBoardLiteComponent.KillerEntityId"/>。<br/>
    /// 应在 <see cref="ImpactSystem"/> 之后更新（§10）。
    /// </summary>
    public sealed class UnitVitalitySystem : IEcsSystem
    {
        public int UpdateOrder => 32;

        private readonly HashSet<long> _deathAnnounced = new HashSet<long>();

        public void Initialize()
        {
        }

        public void Destroy()
        {
            _deathAnnounced.Clear();
        }

        public void Update()
        {
            foreach (var ecs in EcsWorld.Instance.GetEntitiesWithComponent<EntityDataComponent>())
            {
                var data = ecs.GetComponent<EntityDataComponent>();
                if (data.GetData(EntityBaseDataCore.CrtHp) > 1e-9)
                {
                    _deathAnnounced.Remove(ecs.Id);
                    continue;
                }

                if (!ecs.HasComponent<CombatBoardLiteComponent>())
                    continue;

                var board = ecs.GetComponent<CombatBoardLiteComponent>();
                if (board.KillerEntityId == 0)
                {
                    board.KillerEntityId = board.LastDamageFromEntityId;
                    ecs.SetComponent(board);
                }

                if (_deathAnnounced.Add(ecs.Id))
                    CombatUnitDeathRelay.AnnounceFirstDeath(ecs, board.KillerEntityId);
            }
        }
    }
}
