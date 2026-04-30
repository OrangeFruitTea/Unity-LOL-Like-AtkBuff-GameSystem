using Core.ECS;

namespace Core.Entity
{
    /// <summary>
    /// 统一生与死门槛；击倒时写入 <see cref="CombatBoardLiteComponent.KillerEntityId"/>。<br/>
    /// 应在 <see cref="ImpactSystem"/> 之后更新（§10）。
    /// </summary>
    public sealed class UnitVitalitySystem : IEcsSystem
    {
        public int UpdateOrder => 32;

        public void Initialize()
        {
        }

        public void Destroy()
        {
        }

        public void Update()
        {
            foreach (var ecs in EcsWorld.Instance.GetEntitiesWithComponent<EntityDataComponent>())
            {
                var data = ecs.GetComponent<EntityDataComponent>();
                if (data.GetData(EntityBaseDataCore.CrtHp) > 1e-9)
                    continue;
                if (!ecs.HasComponent<CombatBoardLiteComponent>())
                    continue;

                var board = ecs.GetComponent<CombatBoardLiteComponent>();
                if (board.KillerEntityId != 0)
                    continue;

                board.KillerEntityId = board.LastDamageFromEntityId;
                ecs.SetComponent(board);
            }
        }
    }
}
