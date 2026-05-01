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

        private void Awake()
        {
            if (entity == null)
                entity = GetComponent<EntityBase>();
            if (animator == null)
                animator = GetComponentInChildren<Animator>();

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
                return;

            StopStrikeFallbackCoroutine();

            _activeStrikeDispatch = dispatch ?? _defaultDispatch;
            _strikeArmed = true;

            animator.SetTrigger(_hashAttack);

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
                return;

            _strikeArmed = false;

            if (!_activeStrikeDispatch.TryDispatchNormalAttack(entity, out var err))
                Debug.Log($"[UnitAnimDrv] strike dispatch failed: {err}");

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

        private void LateUpdate()
        {
            if (_dead || animator == null)
                return;

            animator.SetFloat(_hashSpeed, ResolvePlanarSpeed());

            if (writeGrounded)
                animator.SetBool(_hashGround, true);
        }

        private float ResolvePlanarSpeed()
        {
            if (navMeshAgent != null && navMeshAgent.isOnNavMesh)
            {
                var v = navMeshAgent.velocity;
                return Mathf.Sqrt(v.x * v.x + v.z * v.z);
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

        private void StopStrikeFallbackCoroutine()
        {
            if (_strikeFallbackCo == null)
                return;
            StopCoroutine(_strikeFallbackCo);
            _strikeFallbackCo = null;
        }
    }
}
