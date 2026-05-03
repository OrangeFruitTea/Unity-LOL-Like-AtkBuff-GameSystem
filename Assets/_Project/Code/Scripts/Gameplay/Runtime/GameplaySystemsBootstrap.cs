using System;
using Core.Combat;
using Core.ECS;
using Core.Entity;
using Core.Entity.Jungle;
using Core.Entity.Minions;
using Gameplay.BuffSystem.Ecs;
using Gameplay.Skill.Conditions;
using Gameplay.Skill.Ecs;
using UnityEngine;

namespace Gameplay.Runtime
{
    /// <summary>
    /// 局内 Gameplay 侧 <see cref="IEcsSystem"/> 统一入口：<see cref="EcsWorld"/> 在 <c>Awake</c> 仅初始化实体管理与表预热，
    /// 本类在 <see cref="RuntimeInitializeLoadType.AfterSceneLoad"/> 注册战斗与技能管线等（与 <see cref="Basement.Runtime.BasementRuntimeBootstrap"/> 同为场景加载后钩子）。
    /// </summary>
    public static class GameplaySystemsBootstrap
    {
        private static bool _registered;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AfterSceneLoad()
        {
            if (_registered)
                return;

            var world = EcsWorld.Instance;
            if (world == null)
            {
                Debug.LogWarning("[GameplaySystemsBootstrap] EcsWorld.Instance 为空，跳过 Gameplay ECS 系统注册。");
                return;
            }

            BuffManager.EnableEcsDriving();

            SkillConditionRegistry.RegisterBuiltInDefaults();

            TryAdd(world, () => new EntitySpawnSystem());
            TryAdd(world, () => new TowerCombatCycleSystem());
            TryAdd(world, () => new ImpactSystem());
            TryAdd(world, () => new UnitVitalitySystem());
            TryAdd(world, () => new BuffTickEcsSystem());
            TryAdd(world, () => new JungleAiSystem());
            TryAdd(world, () => new LaneMinionMoveSystem());
            TryAdd(world, () => new SkillCastPipelineSystem());

            _registered = true;
            Debug.Log("[GameplaySystemsBootstrap] Gameplay IEcsSystem 已注册（含 BuffTick、SkillCastPipeline）。");
        }

        private static void TryAdd<T>(EcsWorld world, Func<T> factory) where T : class, IEcsSystem
        {
            if (world.GetEcsSystem<T>() != null)
                return;
            world.AddEcsSystem(factory());
        }
    }
}
