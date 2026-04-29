using System.Collections.Generic;
using Gameplay.Skill.Config;
using UnityEngine;

namespace Gameplay.Skill.Runtime
{
    internal sealed class ExecutionBatch
    {
        public float FireTimeFromCastStart { get; set; }
        public List<int> StepIndices { get; } = new List<int>();
    }

    /// <summary>
    /// 将步骤表折叠为按时间触发的批次（同 <see cref="BuffApplicationTriggerKind.Immediate"/> / <see cref="BuffApplicationTriggerKind.AfterDelay"/>）。
    /// </summary>
    internal static class PipelineScheduleBuilder
    {
        public static List<ExecutionBatch> BuildTimedBatches(SkillDefinition def)
        {
            var result = new List<ExecutionBatch>();
            if (def?.Steps == null || def.Steps.Count == 0)
                return result;

            var entries = new List<(float t, int idx)>();
            for (int i = 0; i < def.Steps.Count; i++)
            {
                var s = def.Steps[i];
                switch (s.TriggerKind)
                {
                    case BuffApplicationTriggerKind.Immediate:
                        entries.Add((0f, i));
                        break;
                    case BuffApplicationTriggerKind.AfterDelay:
                        entries.Add((Mathf.Max(0f, s.DelaySecondsFromCastStart), i));
                        break;
                }
            }

            entries.Sort((a, b) =>
            {
                int c = a.t.CompareTo(b.t);
                return c != 0 ? c : a.idx.CompareTo(b.idx);
            });

            int k = 0;
            while (k < entries.Count)
            {
                float t = entries[k].t;
                var batch = new ExecutionBatch { FireTimeFromCastStart = t };
                while (k < entries.Count && Mathf.Approximately(entries[k].t, t))
                {
                    batch.StepIndices.Add(entries[k].idx);
                    k++;
                }

                result.Add(batch);
            }

            return result;
        }

        public static List<int> CollectConditionStepIndices(SkillDefinition def)
        {
            var list = new List<int>();
            if (def?.Steps == null)
                return list;
            for (int i = 0; i < def.Steps.Count; i++)
            {
                if (def.Steps[i].TriggerKind == BuffApplicationTriggerKind.OnCondition)
                    list.Add(i);
            }

            return list;
        }

        public static List<int> CollectEventStepIndices(SkillDefinition def)
        {
            var list = new List<int>();
            if (def?.Steps == null)
                return list;
            for (int i = 0; i < def.Steps.Count; i++)
            {
                if (def.Steps[i].TriggerKind == BuffApplicationTriggerKind.OnEvent)
                    list.Add(i);
            }

            return list;
        }
    }
}
