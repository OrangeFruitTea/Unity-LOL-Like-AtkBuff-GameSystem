using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.Skill.Runtime
{
    /// <summary>
    /// 简易技能冷却（按施法者 ECS 实体 id + 技能 id）；联机/存档可替换为 ECS 组件实现。
    /// </summary>
    public static class SkillCooldownTracker
    {
        private static readonly Dictionary<(long casterEcsId, int skillId), float> NextReadyUnityTime =
            new Dictionary<(long, int), float>();

        public static void Clear()
        {
            NextReadyUnityTime.Clear();
        }

        public static bool IsReady(long casterEcsId, int skillId)
        {
            if (!NextReadyUnityTime.TryGetValue((casterEcsId, skillId), out var t))
                return true;
            return Time.time >= t;
        }

        public static void NotifyCast(long casterEcsId, int skillId, float cooldownSeconds)
        {
            if (cooldownSeconds <= 0f)
                return;
            NextReadyUnityTime[(casterEcsId, skillId)] = Time.time + cooldownSeconds;
        }
    }
}
