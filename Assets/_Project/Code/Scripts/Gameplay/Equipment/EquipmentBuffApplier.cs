using System.Collections.Generic;
using Core.Entity;
using Gameplay.Equipment.Config;
using Gameplay.Skill.Buff;
using Gameplay.Skill.Config;
using UnityEngine;

namespace Gameplay.Equipment
{
    /// <summary>
    /// 装备穿脱时：按 <see cref="ItemConfigDefinition.EquippedBuffs"/> 调用 <see cref="BuffApplyService"/>，
    /// 并通过 <see cref="BuffManager"/> 观察回调捕获 <see cref="BuffBase"/> 引用供卸下移除。
    /// </summary>
    public static class EquipmentBuffApplier
    {
        /// <summary>
        /// 对持有者施加该实例对应配置中的全部 Buff；成功后在 <paramref name="instance"/> 内登记 binding。
        /// </summary>
        /// <remarks>
        /// 若 Buff 冲突策略为 <c>Combine</c> 且未新建实例，<see cref="BuffManager"/> 可能不会触发「新增」回调，
        /// 此时无法捕获引用，会打警告且该 binding 无法靠引用移除（需后续扩展 BuffManager 或调整 Buff 冲突配置）。
        /// </remarks>
        public static bool TryApplyEquippedBuffs(
            EquipmentInstance instance,
            in EquipmentEquipOptions options,
            IReadOnlyList<IBuffApplicationModifier> modifiers,
            out string error)
        {
            error = null;
            if (instance?.Owner == null)
            {
                error = "instance or owner is null";
                return false;
            }

            if (!EquipmentCatalog.TryGet(instance.ItemConfigId, out var item))
            {
                error = $"ItemConfig not found: {instance.ItemConfigId}";
                return false;
            }

            if (instance.BuffByBindingForDebug.Count > 0)
            {
                error = "equipment instance already has applied buff bindings; call RemoveEquippedBuffs first";
                return false;
            }

            var owner = instance.Owner;
            var appliedThisEquip = new List<(string bindingId, BuffBase buff)>();

            try
            {
                var bindings = item.EquippedBuffs;
                if (bindings == null || bindings.Count == 0)
                    return true;

                foreach (var binding in bindings)
                {
                    if (binding == null || string.IsNullOrEmpty(binding.BindingId))
                    {
                        error = "invalid binding (null or empty bindingId)";
                        if (options.StrictEquipmentBuffApply)
                        {
                            RollbackApplied(owner, appliedThisEquip);
                            instance.ClearAppliedBuffMap();
                            return false;
                        }

                        continue;
                    }

                    if (options.ValidateAgainstBuffData && BuffDataLoader.Instance != null &&
                        !BuffDataLoader.Instance.TryGet(binding.BuffId, out _))
                    {
                        error = $"BuffData missing buffId={binding.BuffId} (binding {binding.BindingId})";
                        Debug.LogWarning($"[EquipmentBuffApplier] {error}");
                        if (options.StrictEquipmentBuffApply)
                        {
                            RollbackApplied(owner, appliedThisEquip);
                            instance.ClearAppliedBuffMap();
                            return false;
                        }

                        continue;
                    }

                    if (options.ValidateRegistry && !BuffTypeRegistry.TryGetFactory(binding.BuffId, out _))
                    {
                        error = $"no IBuffFactory for buffId={binding.BuffId} (binding {binding.BindingId})";
                        Debug.LogError($"[EquipmentBuffApplier] {error}");
                        if (options.StrictEquipmentBuffApply)
                        {
                            RollbackApplied(owner, appliedThisEquip);
                            instance.ClearAppliedBuffMap();
                            return false;
                        }

                        continue;
                    }

                    var req = CreateRequest(binding, owner, options.ItemTier);
                    BuffBase captured = null;
                    void Handler(BuffBase b) => captured = b;

                    BuffManager.Instance.StartObserving(owner, Handler);
                    try
                    {
                        if (!BuffApplyService.TryApply(req, modifiers, out var applyErr))
                        {
                            error = applyErr;
                            if (options.StrictEquipmentBuffApply)
                            {
                                RollbackApplied(owner, appliedThisEquip);
                                instance.ClearAppliedBuffMap();
                                return false;
                            }

                            continue;
                        }
                    }
                    finally
                    {
                        BuffManager.Instance.StopObserving(owner, Handler);
                    }

                    if (captured == null)
                    {
                        Debug.LogWarning(
                            $"[EquipmentBuffApplier] 施加成功但未捕获 Buff 引用 binding={binding.BindingId} buffId={binding.BuffId}（可能为 Combine 叠层）");
                        if (options.StrictEquipmentBuffApply)
                        {
                            error = $"strict mode: could not capture buff ref for binding={binding.BindingId}";
                            RollbackApplied(owner, appliedThisEquip);
                            instance.ClearAppliedBuffMap();
                            return false;
                        }

                        continue;
                    }

                    appliedThisEquip.Add((binding.BindingId, captured));
                    instance.RegisterAppliedBuff(binding.BindingId, captured);
                }

                return true;
            }
            catch
            {
                RollbackApplied(owner, appliedThisEquip);
                instance.ClearAppliedBuffMap();
                throw;
            }
        }

        /// <summary> 移除该实例在穿时登记的全部 Buff（按引用调用 <see cref="BuffManager.RemoveBuff"/>）。 </summary>
        public static void RemoveEquippedBuffs(EquipmentInstance instance)
        {
            if (instance?.Owner == null)
                return;

            var owner = instance.Owner;
            foreach (var kv in instance.BuffByBindingForDebug)
            {
                var buff = kv.Value;
                if (buff == null)
                    continue;
                if (!BuffManager.Instance.RemoveBuff(owner, buff))
                    Debug.LogWarning($"[EquipmentBuffApplier] RemoveBuff failed binding={kv.Key} owner={owner}");
            }

            instance.ClearAppliedBuffMap();
        }

        private static void RollbackApplied(EntityBase owner, List<(string bindingId, BuffBase buff)> applied)
        {
            for (int i = applied.Count - 1; i >= 0; i--)
            {
                var buff = applied[i].buff;
                if (buff != null)
                    BuffManager.Instance.RemoveBuff(owner, buff);
            }

            applied.Clear();
        }

        private static BuffApplyRequest CreateRequest(EquipmentBuffBindingDefinition binding, EntityBase owner, uint itemTier)
        {
            uint level = binding.BuffLevel + binding.LevelScalingPerItemTier * itemTier;
            var step = new BuffApplicationStepDefinition
            {
                BuffId = binding.BuffId,
                DurationOverride = binding.DurationOverride,
                CustomArgs = binding.CustomArgs
            };
            return BuffApplyRequest.FromStep(step, owner, owner, level);
        }
    }
}
