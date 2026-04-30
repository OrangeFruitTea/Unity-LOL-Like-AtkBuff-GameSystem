using Core.ECS;

namespace Core.Entity
{
    /// <summary> 阵营；局内 combat 共用。设计文档 §4.1。 </summary>
    public struct FactionComponent : IEcsComponent
    {
        public FactionTeamId TeamId;

        public void InitializeDefaults()
        {
            TeamId = FactionTeamId.Neutral;
        }
    }
}
