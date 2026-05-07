using Core.Entity;
using Core.ECS;
using Gameplay.Presentation;
using UnityEngine;

namespace Gameplay.Combat.Targeting
{
    /// <summary>MVP：<c>Acquire</c> → Commit 黑板 → <see cref="ICombatImpactDispatch"/>（仅从板读受害者）。</summary>
    [DisallowMultipleComponent]
    public sealed class MvpHeroBasicAttackDebugBridge : MonoBehaviour
    {
        [SerializeField] private EntityBase attacker;
        [SerializeField] private KeyCode attackNearestKey = KeyCode.A;
        [SerializeField] private KeyCode attackBoardHintKey = KeyCode.Return;
        [SerializeField] private float attackCooldownSeconds = 0.35f;

        [Header("Debug")]
        [SerializeField] private bool logCombatPipeline = true;

        private ITargetAcquisitionService _acquisition;
        private ICombatImpactDispatch _dispatch;
        private float _nextSwingTime;

        private void Awake()
        {
            if (attacker == null)
                attacker = GetComponent<EntityBase>();

            _acquisition = new DefaultTargetAcquisitionService();
            _dispatch = new DefaultCombatImpactDispatch();
        }

        private void Update()
        {
            if (attacker == null || Time.time < _nextSwingTime)
                return;

            if (Input.GetKeyDown(attackNearestKey))
                TrySwingNearest();

            if (Input.GetKeyDown(attackBoardHintKey))
                TrySwingBoardHint();
        }

        private void TrySwingNearest()
        {
            if (logCombatPipeline)
                Debug.Log(
                    $"[MvpHeroBasicAttack] ▶ Key={attackNearestKey} NearestHostile | attacker={attacker.name} ecs={attacker.BoundEcsEntity.Id}");

            var req = new TargetAcquisitionRequest(TargetingShapeKind.NearestInSphere, attacker, rangeOrRadius: 0f);
            var result = _acquisition.Acquire(req);
            AfterAcquireCommitAndDispatch(result);
        }

        private void TrySwingBoardHint()
        {
            long hint = ReadBoardAttackHint(attacker);
            if (logCombatPipeline)
                Debug.Log($"[MvpHeroBasicAttack] ▶ Key={attackBoardHintKey} BoardHint id={hint} | attacker={attacker.name}");

            var req = new TargetAcquisitionRequest(TargetingShapeKind.PointEntity, attacker, hint, rangeOrRadius: 0f);
            var result = _acquisition.Acquire(req);
            AfterAcquireCommitAndDispatch(result);
        }

        /// <summary>将解析结果写入板后 Strike 只认黑板。</summary>
        private void AfterAcquireCommitAndDispatch(TargetAcquisitionResult result)
        {
            if (!result.Succeeded)
            {
                if (logCombatPipeline)
                    Debug.Log($"[MvpHeroBasicAttack] ✖ acquire failed: {result.Error}");
                return;
            }

            if (result.Hits.Count < 1)
            {
                if (logCombatPipeline)
                    Debug.Log("[MvpHeroBasicAttack] ✖ acquire ok but Hits empty");
                return;
            }

            var vic = result.SuggestedPrimary != null ? result.SuggestedPrimary : result.Hits[0];
            long id = vic.BoundEcsEntity.Id;
            if (id == 0)
            {
                if (logCombatPipeline)
                    Debug.Log("[MvpHeroBasicAttack] ✖ victim has no ecs id");
                return;
            }

            if (logCombatPipeline)
                Debug.Log(
                    $"[MvpHeroBasicAttack] ✓ target={vic.name} victimEcs={id} | commit CombatBoard → swing/dispatch");

            if (!CombatBoardTargetSync.SetAttackAndThreatSameTarget(attacker, id))
            {
                Debug.LogWarning($"{nameof(MvpHeroBasicAttackDebugBridge)}: SetAttackAndThreatSameTarget failed (CombatBoard?).");
                return;
            }

            var anim = attacker.GetComponent<UnitAnimDrv>();
            if (anim != null)
            {
                anim.BeginNormalAttackSwing(_dispatch);
            }
            else if (!_dispatch.TryDispatchNormalAttack(attacker, out var err))
            {
                if (logCombatPipeline)
                    Debug.Log($"[MvpHeroBasicAttack] ✖ dispatch failed: {err}");
                return;
            }

            _nextSwingTime = Time.time + attackCooldownSeconds;
            if (logCombatPipeline)
                Debug.Log(
                    $"[MvpHeroBasicAttack] ✓ {(anim != null ? "swing queued (等 AnimEvt_Strike 或 Fallback 出伤)" : "已直接 DispatchImpact")} | victimEcs={id}");
        }

        private static long ReadBoardAttackHint(EntityBase caster)
        {
            if (caster == null)
                return 0;

            var ecs = caster.BoundEcsEntity;
            if (!ecs.IsValid() || !ecs.HasComponent<CombatBoardLiteComponent>())
                return 0;

            return ecs.GetComponent<CombatBoardLiteComponent>().AttackTargetEntityId;
        }
    }
}
