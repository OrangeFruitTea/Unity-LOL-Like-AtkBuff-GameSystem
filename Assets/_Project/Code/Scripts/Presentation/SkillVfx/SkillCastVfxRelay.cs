using System.Collections;
using System.Collections.Generic;
using Gameplay.Skill.Context;
using UnityEngine;
using UnityEngine.AI;

namespace Gameplay.Presentation.SkillVfx
{
    /// <summary>
    /// 方案 A：英雄（或任意施法者）身上配置 skillId → 特效 Prefab；由 <see cref="Gameplay.Skill.SkillExecutionFacade"/> 施法成功后驱动播放。<br/>
    /// 手部锚点可在本组件上拖引用；未赋值时回退到施法者根节点。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SkillCastVfxRelay : MonoBehaviour
    {
        [SerializeField]
        private List<SkillVfxSpawnSpec> specs = new List<SkillVfxSpawnSpec>();

        [SerializeField]
        private Transform casterHandRight;

        [SerializeField]
        private Transform casterHandLeft;

        [SerializeField]
        private float navMeshSampleRadius = 4f;

        [SerializeField]
        private bool logWarnings = true;

        /// <summary>由 <see cref="Gameplay.Skill.SkillExecutionFacade"/> 在 TryBeginCast 成功后调用。</summary>
        public void NotifyCastSucceeded(int skillId, SkillCastContext context)
        {
            if (context == null || specs == null || specs.Count == 0)
                return;

            for (var i = 0; i < specs.Count; i++)
            {
                var spec = specs[i];
                if (spec.skillId != skillId || spec.fxPrefab == null)
                    continue;

                if (spec.moment == SkillFxMoment.OnCastSucceeded)
                    SpawnOne(in spec, context);
                else if (spec.moment == SkillFxMoment.AfterDelaySeconds && isActiveAndEnabled)
                    StartCoroutine(SpawnAfterDelay(in spec, context, Mathf.Max(0f, spec.delaySeconds)));
            }
        }

        private IEnumerator SpawnAfterDelay(SkillVfxSpawnSpec spec, SkillCastContext context, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (!IsContextUsable(context))
                yield break;
            SpawnOne(in spec, context);
        }

        private void SpawnOne(in SkillVfxSpawnSpec spec, SkillCastContext context)
        {
            if (!IsContextUsable(context))
                return;

            if (!TryResolveAnchor(in spec, context, out var attachTr, out var worldPos, out var worldRot))
                return;

            var instance = Instantiate(spec.fxPrefab);
            instance.transform.SetPositionAndRotation(worldPos, worldRot);

            if (spec.parentToAttachTransform && attachTr != null)
                instance.transform.SetParent(attachTr, true);
        }

        private bool IsContextUsable(SkillCastContext context)
        {
            return context != null
                   && context.Caster != null
                   && context.Caster.gameObject.activeInHierarchy;
        }

        private bool TryResolveAnchor(
            in SkillVfxSpawnSpec spec,
            SkillCastContext context,
            out Transform attachTransform,
            out Vector3 worldPosition,
            out Quaternion worldRotation)
        {
            attachTransform = null;
            worldPosition = Vector3.zero;
            worldRotation = Quaternion.identity;

            var caster = context.Caster.transform;

            switch (spec.attach)
            {
                case SkillFxAttachKind.CasterRoot:
                    attachTransform = caster;
                    worldPosition = caster.TransformPoint(spec.localPositionOffset);
                    worldRotation = ResolveRotation(spec, caster.rotation, caster.rotation);
                    return true;

                case SkillFxAttachKind.CasterHandRight:
                    attachTransform = casterHandRight != null ? casterHandRight : caster;
                    worldPosition = attachTransform.TransformPoint(spec.localPositionOffset);
                    worldRotation = ResolveRotation(spec, attachTransform.rotation, caster.rotation);
                    return true;

                case SkillFxAttachKind.CasterHandLeft:
                    attachTransform = casterHandLeft != null ? casterHandLeft : caster;
                    worldPosition = attachTransform.TransformPoint(spec.localPositionOffset);
                    worldRotation = ResolveRotation(spec, attachTransform.rotation, caster.rotation);
                    return true;

                case SkillFxAttachKind.GroundUnderCaster:
                    attachTransform = null;
                    worldPosition = SampleGround(caster.position + caster.TransformVector(spec.localPositionOffset));
                    worldRotation = ResolveRotation(spec, caster.rotation, caster.rotation);
                    return true;

                case SkillFxAttachKind.PrimaryTargetRoot:
                    if (context.PrimaryTarget == null)
                    {
                        if (logWarnings)
                            Debug.LogWarning($"{nameof(SkillCastVfxRelay)}: PrimaryTarget missing for skillFx skillId={spec.skillId} ({name})");
                        return false;
                    }

                    attachTransform = context.PrimaryTarget.transform;
                    worldPosition = attachTransform.TransformPoint(spec.localPositionOffset);
                    worldRotation = ResolveRotation(spec, attachTransform.rotation, caster.rotation);
                    return true;

                case SkillFxAttachKind.GroundUnderPrimaryTarget:
                    if (context.PrimaryTarget == null)
                    {
                        if (logWarnings)
                            Debug.LogWarning($"{nameof(SkillCastVfxRelay)}: PrimaryTarget missing for ground fx skillId={spec.skillId} ({name})");
                        return false;
                    }

                    attachTransform = null;
                    worldPosition = SampleGround(
                        context.PrimaryTarget.transform.position +
                        context.PrimaryTarget.transform.TransformVector(spec.localPositionOffset));
                    worldRotation = ResolveRotation(spec, caster.rotation, caster.rotation);
                    return true;

                default:
                    return false;
            }
        }

        private Vector3 SampleGround(Vector3 raw)
        {
            return NavMesh.SamplePosition(raw, out var hit, navMeshSampleRadius, NavMesh.AllAreas)
                ? hit.position
                : raw;
        }

        private static Quaternion ResolveRotation(in SkillVfxSpawnSpec spec, Quaternion attachRotation, Quaternion casterRotation)
        {
            var localRot = Quaternion.Euler(spec.localEulerAngles);
            if (spec.multiplyAttachRotation)
                return attachRotation * localRot;
            return localRot;
        }
    }
}
