using Core.ECS;

namespace Gameplay.Skill.Ecs
{
    /// <summary>
    /// 施法者身上标记是否存在进行中的技能管线（详细状态在 <see cref="SkillCastPipelineSystem"/> 会话中）。
    /// </summary>
    public struct SkillPipelineRuntimeComponent : IEcsComponent
    {
        public bool HasActiveCast;

        public void InitializeDefaults()
        {
            HasActiveCast = false;
        }
    }
}
