using System.Collections;
using Core.Entity;
using Core.ECS;
using Gameplay.Combat.Targeting;
using UnityEngine;
using UnityEngine.AI;

namespace Gameplay.Presentation
{
    /// <summary>
    /// P0/P1：单机位移动画参数 + 普攻（动画事件落点出伤）+ 死亡 + 技能 Trigger + 受击（Animator + 可选 UI 接口）。<br/>
    /// Animation Clip 事件：挂载 <see cref="AnimEvt_Strike"/>。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UnitAnimDrv : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private EntityBase entity;
        [SerializeField] private Animator animator;
        [SerializeField] private NavMeshAgent navMeshAgent;
        [SerializeField] private Rigidbody rigidBody;
        [SerializeField] private CharacterController characterController;

        [Header("Locomotion blend (Animator Speed)")]
        [Tooltip("与 Locomotion 1D Blend 阈值 0 / 0.1 / 1 对齐：将平面速度除以此值再 Clamp 到 0～1 写入 Animator。应 ≥ 本单位 NavMeshAgent.speed。")]
        [SerializeField] private float locomotionMaxSpeedForAnimator = 4f;

        [Tooltip("velocity 很小但仍有路径时，用 desiredVelocity 估计移动，减轻卡在 Idle 的概率。")]
        [SerializeField] private bool useDesiredVelocityWhenNavVelocityLow = true;

        [Tooltip("低于该平面速度视为静止，避免噪声。略大于 0 即可。")]
        [SerializeField] private float navPlanarSpeedEpsilon = 0.02f;

        [Header("Animator 驱动（Nav + 子物体 Animator）")]
        [Tooltip("关闭 Root Motion，由 NavMeshAgent 驱动物体根位移，避免与导航抢 Transform 导致状态机异常。")]
        [SerializeField] private bool animatorDisableRootMotion = true;

        [Tooltip("避免移动时按渲染裁剪停止更新 Animator（子物体在 entity-model 下时尤其重要）。")]
        [SerializeField] private bool animatorAlwaysAnimate = true;

        [Tooltip("用根节点水平位移 / deltaTime 与 Nav/velocity 估计取较大值，应对 agent.velocity 长期为 0 等情况。")]
        [SerializeField] private bool useMaxOfNavAndTransformPlanarSpeed = true;

        [Header("Animator param names")]
        [SerializeField] private string paramSpeed = "Speed";
        [SerializeField] private string paramIsDead = "IsDead";
        [SerializeField] private string paramGrounded = "IsGrounded";

        [SerializeField] private bool writeGrounded = true;

        [Header("Triggers")]
        [SerializeField] private string triggerAttack = "Attack";
        [SerializeField] private string triggerSkill = "Skill";
        [SerializeField] private string triggerHit = "Hit";

        [Header("Normal attack strike")]
        [Tooltip("-1：仅 AnimEvt_Strike / 不写 Fallback；≥0：延迟后强制执行一次普攻 Dispatch")]
        [SerializeField]
        private float strikeFallbackDelaySeconds = -1f;

        [SerializeField] private MonoBehaviour hpBarFxHost;

        [Header("Debug")]
        [SerializeField] private bool logCombatPipeline = true;

        private readonly ICombatImpactDispatch _defaultDispatch = new DefaultCombatImpactDispatch();

        private ICombatImpactDispatch _activeStrikeDispatch;

        private int _hashSpeed;
        private int _hashIsDead;
        private int _hashGround;
        private int _hashAttack;
        private int _hashSkill;
        private int _hashHit;

        private bool _dead;
        private bool _strikeArmed;

        private Coroutine _strikeFallbackCo;

        private Vector2 _lastRootPlanarPosition;
        private bool _hasLastRootPlanarPosition;

        private void Awake()
        {
            if (entity == null)
                entity = GetComponent<EntityBase>();
            if (animator == null)
                animator = GetComponentInChildren<Animator>(true);

            ApplyAnimatorDriveSettings(animator);

            ResolveOptionalMotionRefs();

            _hashSpeed = Animator.StringToHash(paramSpeed);
            _hashIsDead = Animator.StringToHash(paramIsDead);
            _hashGround = Animator.StringToHash(paramGrounded);
            _hashAttack = Animator.StringToHash(triggerAttack);
            _hashSkill = Animator.StringToHash(triggerSkill);
            _hashHit = Animator.StringToHash(triggerHit);

            _activeStrikeDispatch = _defaultDispatch;
        }

        private void OnEnable()
        {
            UnitDeathEventHub.UnitDied += OnUnitDiedHub;
        }

        private void OnDisable()
        {
            UnitDeathEventHub.UnitDied -= OnUnitDiedHub;
            StopStrikeFallbackCoroutine();
        }

        private void ResolveOptionalMotionRefs()
        {
            if (navMeshAgent == null)
                navMeshAgent = GetComponent<NavMeshAgent>();
            if (rigidBody == null)
                rigidBody = GetComponent<Rigidbody>();
            if (characterController == null)
                characterController = GetComponent<CharacterController>();
        }

        /// <summary>
        /// 黑板已由外部写好：播 Attack。出伤在动画事件 <see cref="AnimEvt_Strike"/>，或 Fallback 超时自动出伤。<br/>
        /// <paramref name="dispatch"/> 为空则用内置 <see cref="DefaultCombatImpactDispatch"/>。
        /// </summary>
        public void BeginNormalAttackSwing(ICombatImpactDispatch dispatch = null)
        {
            if (_dead || animator == null)
            {
                if (logCombatPipeline)
                    Debug.Log($"[UnitAnimDrv] BeginNormalAttackSwing 跳过: dead={_dead} animatorNull={animator == null} ({name})");
                return;
            }

            StopStrikeFallbackCoroutine();

            _activeStrikeDispatch = dispatch ?? _defaultDispatch;
            _strikeArmed = true;

            animator.SetTrigger(_hashAttack);

            if (logCombatPipeline)
            {
                var fb = strikeFallbackDelaySeconds >= 0f ? $"{strikeFallbackDelaySeconds:F2}s 后 Fallback 出伤" : "无 Fallback（须 Attack clip 上 AnimEvt_Strike）";
                Debug.Log($"[UnitAnimDrv] Attack Trigger 已发 | {name} ecs={entity.BoundEcsEntity.Id} | {fb}");
            }

            if (strikeFallbackDelaySeconds >= 0f && isActiveAndEnabled)
                _strikeFallbackCo = StartCoroutine(StrikeFallbackCoroutine(strikeFallbackDelaySeconds));
        }

        private IEnumerator StrikeFallbackCoroutine(float delay)
        {
            yield return new WaitForSeconds(delay);
            _strikeFallbackCo = null;
            TryStrikeOnceInternal();
        }

        /// <summary>供 Animation Clip 事件调用（无参）。</summary>
        public void AnimEvt_Strike() => TryStrikeOnceInternal();

        private void TryStrikeOnceInternal()
        {
            StopStrikeFallbackCoroutine();

            if (!_strikeArmed || entity == null || _dead || animator == null)
            {
                if (logCombatPipeline && entity != null)
                    Debug.Log(
                        $"[UnitAnimDrv] TryStrikeOnceInternal 跳过 armed={_strikeArmed} dead={_dead} animatorNull={animator == null} ({name})");
                return;
            }

            _strikeArmed = false;

            if (logCombatPipeline)
                Debug.Log($"[UnitAnimDrv] ▶ AnimEvt_Strike / Fallback → TryDispatchNormalAttack | {name}");

            if (!_activeStrikeDispatch.TryDispatchNormalAttack(entity, out var err))
            {
                Debug.Log($"[UnitAnimDrv] ✖ strike dispatch failed: {err}");
                _activeStrikeDispatch = _defaultDispatch;
                return;
            }

            if (logCombatPipeline)
                Debug.Log($"[UnitAnimDrv] ✓ TryDispatchNormalAttack 成功（待 ImpactSystem 结算 HP）| {name}");

            _activeStrikeDispatch = _defaultDispatch;
        }

        /// <summary>P1：技能施法成功后可调；已由 <see cref="SkillExecutionFacade"/> 在进入管线成功后自动触发。</summary>
        public void NotifySkillCastStarted()
        {
            if (_dead || animator == null)
                return;
            animator.SetTrigger(_hashSkill);
        }

        /// <summary>P1：受击 Animator Trigger + 可选 <see cref="IUnitHpBarFeedback"/>。</summary>
        public void NotifyDamaged()
        {
            if (_dead || animator == null)
                return;
            animator.SetTrigger(_hashHit);

            if (hpBarFxHost is IUnitHpBarFeedback fx)
                fx.OnHpDamagedShake();
        }

        private void Start()
        {
            // UnitModelAssembler 等为 DefaultExecutionOrder 早于本脚本，但若此后仍有模型替换，延后一帧再绑一次 Animator。
            StartCoroutine(ResolveAnimatorNextFrame());
        }

        private IEnumerator ResolveAnimatorNextFrame()
        {
            yield return null;
            EnsureAnimatorReference();
        }

        private void LateUpdate()
        {
            EnsureAnimatorReference();

            if (_dead || animator == null)
                return;

            animator.SetFloat(_hashSpeed, ResolveAnimatorSpeedParam());

            if (writeGrounded)
                animator.SetBool(_hashGround, true);
        }

        private void EnsureAnimatorReference()
        {
            if (animator != null && animator.gameObject.activeInHierarchy)
                return;

            animator = GetComponentInChildren<Animator>(true);
            ApplyAnimatorDriveSettings(animator);
        }

        private void ApplyAnimatorDriveSettings(Animator anim)
        {
            if (anim == null)
                return;

            if (animatorDisableRootMotion)
                anim.applyRootMotion = false;
            if (animatorAlwaysAnimate)
                anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }

        /// <summary>0～1，与 Controller 里 Locomotion BlendTree 阈值一致。</summary>
        private float ResolveAnimatorSpeedParam()
        {
            var planar = ResolvePlanarSpeedMetersPerSecond();
            if (locomotionMaxSpeedForAnimator <= 1e-5f)
                return 0f;
            return Mathf.Clamp01(planar / locomotionMaxSpeedForAnimator);
        }

        private float ResolvePlanarSpeedMetersPerSecond()
        {
            float fromMotion = ResolvePlanarSpeedFromAgentOrPhysics();

            if (!useMaxOfNavAndTransformPlanarSpeed)
                return fromMotion;

            var root = transform;
            var p = root.position;
            var planarNow = new Vector2(p.x, p.z);
            float fromDelta = 0f;
            if (_hasLastRootPlanarPosition && Time.deltaTime > 1e-5f)
            {
                fromDelta = (planarNow - _lastRootPlanarPosition).magnitude / Time.deltaTime;
            }

            _lastRootPlanarPosition = planarNow;
            _hasLastRootPlanarPosition = true;

            return Mathf.Max(fromMotion, fromDelta);
        }

        private float ResolvePlanarSpeedFromAgentOrPhysics()
        {
            if (navMeshAgent != null && navMeshAgent.isOnNavMesh)
            {
                var v = navMeshAgent.velocity;
                var planar = Mathf.Sqrt(v.x * v.x + v.z * v.z);
                if (useDesiredVelocityWhenNavVelocityLow &&
                    planar < navPlanarSpeedEpsilon &&
                    navMeshAgent.hasPath &&
                    !navMeshAgent.isStopped)
                {
                    var d = navMeshAgent.desiredVelocity;
                    planar = Mathf.Sqrt(d.x * d.x + d.z * d.z);
                }

                return planar;
            }

            if (rigidBody != null && !rigidBody.isKinematic)
            {
                var v = rigidBody.velocity;
                return Mathf.Sqrt(v.x * v.x + v.z * v.z);
            }

            if (characterController != null)
            {
                var v = characterController.velocity;
                return Mathf.Sqrt(v.x * v.x + v.z * v.z);
            }

            return 0f;
        }

        private void OnUnitDiedHub(EcsEntity victim, long killerEntityId)
        {
            _ = killerEntityId;
            if (!victim.IsValid() || entity == null)
                return;
            if (entity.BoundEcsEntity.Id == 0 || victim.Id != entity.BoundEcsEntity.Id)
                return;

            _dead = true;
            _strikeArmed = false;
            StopStrikeFallbackCoroutine();

            if (animator == null)
                return;

            animator.SetBool(_hashIsDead, true);
        }

        /// <summary>软复活：清除本地死亡门闸并复位 Animator（由 <see cref="Core.Entity.UnitDeathRespawnBehaviour"/> 等调用）。</summary>
        public void NotifyRevived()
        {
            _dead = false;
            _strikeArmed = false;
            StopStrikeFallbackCoroutine();

            if (animator != null)
                animator.SetBool(_hashIsDead, false);
        }

        private void StopStrikeFallbackCoroutine()
        {
            if (_strikeFallbackCo == null)
                return;
            StopCoroutine(_strikeFallbackCo);
            _strikeFallbackCo = null;
        }
    }
}

