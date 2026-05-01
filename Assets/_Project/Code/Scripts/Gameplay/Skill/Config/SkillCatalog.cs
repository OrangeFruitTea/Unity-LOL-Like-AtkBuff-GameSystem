using System.Collections.Generic;

namespace Gameplay.Skill.Config
{
    /// <summary>
    /// 技能定义只读目录（由 <see cref="Loading.SkillDataLoader"/> 或 <see cref="Core.ECS.EcsWorld.TryReloadSkillCatalogFromStreaming"/> 填充）。
    /// </summary>
    public static class SkillCatalog
    {
        private static readonly Dictionary<int, SkillDefinition> ById = new Dictionary<int, SkillDefinition>();

        public static void Clear()
        {
            ById.Clear();
        }

        public static void Register(SkillDefinition def)
        {
            if (def == null)
                return;
            ById[def.SkillId] = def;
        }

        public static void ReplaceAll(IEnumerable<SkillDefinition> definitions)
        {
            ById.Clear();
            if (definitions == null)
                return;
            foreach (var d in definitions)
            {
                if (d != null)
                    ById[d.SkillId] = d;
            }
        }

        public static bool TryGet(int skillId, out SkillDefinition definition) =>
            ById.TryGetValue(skillId, out definition);

        public static IReadOnlyDictionary<int, SkillDefinition> All => ById;
    }
}
