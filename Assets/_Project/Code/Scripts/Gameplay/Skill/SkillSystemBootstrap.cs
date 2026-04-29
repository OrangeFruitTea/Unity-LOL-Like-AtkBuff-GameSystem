using Core.ECS;
using Gameplay.Skill.Conditions;
using Gameplay.Skill.Ecs;
using UnityEngine;

namespace Gameplay.Skill
{
    /// <summary>
    /// 注册内建条件与 <see cref="SkillCastPipelineSystem"/>（AfterSceneLoad，依赖已存在的 <see cref="EcsWorld"/>）。
    /// </summary>
    public static class SkillSystemBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void RegisterAfterSceneLoad()
        {
            SkillConditionRegistry.RegisterBuiltInDefaults();

            var world = EcsWorld.Instance;
            if (world == null)
            {
                Debug.LogWarning("[SkillSystemBootstrap] EcsWorld.Instance 为空，跳过管线系统注册。");
                return;
            }

            if (world.GetEcsSystem<SkillCastPipelineSystem>() != null)
                return;

            world.AddEcsSystem(new SkillCastPipelineSystem());
            Debug.Log("[SkillSystemBootstrap] SkillCastPipelineSystem 已注册。");
        }
    }
}
