using System.Collections.Generic;
using Core.Entity;
using Gameplay.Skill.Config;
using UnityEngine;

namespace Gameplay.Skill.Buff
{
    /// <summary>
    /// 表驱动 Buff 施加唯一推荐入口；解析注册表并调用 <see cref="BuffManager"/>。
    /// </summary>
    public static class BuffApplyService
    {
        public static bool TryApply(
            BuffApplyRequest request,
            IReadOnlyList<IBuffApplicationModifier> modifiers = null,
            out string error)
        {
            error = null;
            if (request?.Target == null)
            {
                error = "target is null";
                return false;
            }

            if (request.Provider == null)
            {
                error = "provider is null";
                return false;
            }

            if (!EntityEcsBridge.IsValidBuffTarget(request.Target))
            {
                error = "target not valid for buff";
                return false;
            }

            if (BuffDataLoader.Instance != null &&
                !BuffDataLoader.Instance.TryGet(request.BuffId, out _))
            {
                Debug.LogWarning($"[BuffApplyService] BuffData 中无 id={request.BuffId}，仍尝试按注册类型施加。");
            }

            if (modifiers != null)
            {
                foreach (var m in modifiers)
                    m?.Modify(request);
            }

            if (!BuffTypeRegistry.TryGetFactory(request.BuffId, out var factory))
            {
                error = $"no IBuffFactory registered for buffId={request.BuffId}";
                Debug.LogError($"[BuffApplyService] {error}");
                return false;
            }

            try
            {
                factory.Apply(
                    request.Target,
                    request.Provider,
                    request.Level,
                    request.DurationOverride,
                    request.CustomArgsTail);
            }
            catch (System.Exception ex)
            {
                error = ex.Message;
                Debug.LogError($"[BuffApplyService] Apply failed: {ex}");
                return false;
            }

            return true;
        }

        public static bool TryApplyStep(
            BuffApplicationStepDefinition step,
            EntityBase target,
            EntityBase provider,
            uint resolvedBuffLevel,
            IReadOnlyList<IBuffApplicationModifier> modifiers = null,
            out string error)
        {
            var req = BuffApplyRequest.FromStep(step, target, provider, resolvedBuffLevel);
            return TryApply(req, modifiers, out error);
        }
    }
}
