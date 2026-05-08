using Core.ECS;
using Core.Entity;
using Gameplay.Presentation;
using Gameplay.Presentation.Interaction;
using UnityEngine;
using UnityEngine.Serialization;

namespace Gameplay.Combat.Targeting
{
    /// <summary>
    /// **按住 A**：显示普攻地面范围（可选世界空间 Canvas+<see cref="AttackRangeWorldUiDiscPresenter"/>，否则 <see cref="GroundWorldLineIndicator"/> 线框）；**松开 A** 即隐藏。**仅在按住期间**可用 **左键(M0)** 点选敌对单位 → 单次普攻。<br/>
    /// <see cref="MeleeStrikeRules"/>；<see cref="CombatBoardTargetSync"/>；Anim/<see cref="DefaultCombatImpactDispatch"/>。<br/>
    /// <see cref="CombatBoardRaySelectTarget"/>：<see cref="suppressStandaloneCombatBoardRayPick"/> 为 true 时左键点选由其接管语义。
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(20)]
    public sealed class HeroAttackRangeSelectBridge : MonoBehaviour
    {
        [SerializeField]
        private EntityBase attacker;

        [Tooltip("可选手动指定；留空则每次需要时自动查找场景中的 GroundWorldLineIndicator 并缓存（适合玩家动态生成）。")]
        [SerializeField]
        private GroundWorldLineIndicator groundIndicator;

        [Tooltip("若指定：按住 A 时用世界 Canvas+Image 贴地展示范围，不再走 LineRenderer 圆环。")]
        [SerializeField]
        private AttackRangeWorldUiDiscPresenter attackRangeUiDisc;

        private static GroundWorldLineIndicator s_cachedSceneGroundIndicator;

        private bool _loggedMissingGroundIndicator;

        [SerializeField]
        private Camera targetCamera;

        private bool _prevAttackRangeHoldKeyHeld;

        [Header("Inputs")]
        [Tooltip("按住：持续显示普攻范围圈；松开：隐藏。")]
        [FormerlySerializedAs("toggleAttackRangeOutlineKey")]
        [SerializeField]
        private KeyCode attackRangeHoldKey = KeyCode.A;

        [Tooltip("普攻范围圈颜色（地面圆环）。")]
        [SerializeField]
        private Color attackRangeRingColor = new Color(1f, 0.85f, 0.35f, 0.92f);

        [Tooltip("点名敌单位：左键。与本项目 MovementController（默认右键寻路 Mouse1）不冲突。")]
        [SerializeField]
        private bool useMouseLeftForTargetPick = true;

        [Tooltip("为 true：同物体 CombatBoardRaySelectTarget 将不再用左键写黑板（由本脚本在展示范围后接管）。")]
        [SerializeField]
        private bool suppressStandaloneCombatBoardRayPick = true;

        [SerializeField]
        private float swingCooldownSeconds = 0.35f;

        [Header("Ray pick")]
        [SerializeField]
        private float rayMaxDistance = 500f;

        [SerializeField]
        private LayerMask unitRaycastMask;

        private ICombatImpactDispatch _dispatch;

        private float _nextSwingTime;

        /// <summary><see cref="attackRangeHoldKey"/> 按住中：可作左键点名。</summary>
        public bool AttackTargetingModeActive { get; private set; }

        private void Awake()
        {
            if (attacker == null)
                attacker = GetComponent<EntityBase>();

            if (targetCamera == null)
                targetCamera = Camera.main;

            if (attackRangeUiDisc == null)
                attackRangeUiDisc = GetComponentInChildren<AttackRangeWorldUiDiscPresenter>(true);

            ResolveGroundIndicator(includeInactive: false);

            _dispatch = new DefaultCombatImpactDispatch();
        }

        private void OnEnable()
        {
            ResolveGroundIndicator(includeInactive: false);
        }

        private void Start()
        {
            ResolveGroundIndicator(includeInactive: false);
        }

        /// <summary>
        /// 场景中唯一 <see cref="GroundWorldLineIndicator"/>（动态生成玩家时 Awake 可能比关卡物体晚，故在 OnEnable/Start/LateUpdate 反复复用）。
        /// </summary>
        private void ResolveGroundIndicator(bool includeInactive)
        {
            if (groundIndicator != null && groundIndicator)
                return;

            if (groundIndicator != null && !groundIndicator)
                groundIndicator = null;

            if (s_cachedSceneGroundIndicator != null && !s_cachedSceneGroundIndicator)
                s_cachedSceneGroundIndicator = null;

            if (groundIndicator != null)
                return;

            if (s_cachedSceneGroundIndicator != null)
            {
                groundIndicator = s_cachedSceneGroundIndicator;
                return;
            }

#pragma warning disable CS0618
            var found = includeInactive
                ? FindObjectOfType<GroundWorldLineIndicator>(true)
                : FindObjectOfType<GroundWorldLineIndicator>();
#pragma warning restore CS0618

            if (found != null)
                s_cachedSceneGroundIndicator = groundIndicator = found;
        }

        private void Update()
        {
            if (!IsAttackerCombatReady())
                return;

            var holdingRangeKey = Input.GetKey(attackRangeHoldKey);

            if (AttackTargetingModeActive && !holdingRangeKey)
                ExitAttackTargetingMode();

            if (holdingRangeKey && !_prevAttackRangeHoldKeyHeld)
            {
                ResolveGroundIndicator(includeInactive: true);
                if (groundIndicator == null &&
                    attackRangeUiDisc == null &&
                    !_loggedMissingGroundIndicator)
                {
                    _loggedMissingGroundIndicator = true;
                    Debug.LogWarning(
                        $"[{nameof(HeroAttackRangeSelectBridge)}] 未指定 {nameof(AttackRangeWorldUiDiscPresenter)} 且场景中找不到 {nameof(GroundWorldLineIndicator)} — 无法显示普攻范围。",
                        this);
                }
            }

            _prevAttackRangeHoldKeyHeld = holdingRangeKey;
            AttackTargetingModeActive = holdingRangeKey;

            if (!AttackTargetingModeActive)
                return;

            if (!(Time.time >= _nextSwingTime && useMouseLeftForTargetPick && Input.GetMouseButtonDown(0)))
                return;

            if (UiPresentationPointerGate.IsPointerOverUi())
                return;

            if (!TryResolveVictimUnderCursor(out var victim))
                return;

            AttemptExecuteNormalAttackTowards(victim);
        }

        private void LateUpdate()
        {
            if (!AttackTargetingModeActive || attacker == null)
                return;

            ResolveGroundIndicator(includeInactive: false);
            if (attackRangeUiDisc == null && groundIndicator == null)
                return;

            RefreshAttackRingVisualization();
        }

        /// <remarks>若同对象挂了 <see cref="CombatBoardRaySelectTarget"/>，且需要禁止其左键选敌，则由该脚本读取本方法。</remarks>
        public bool SuppressesStandaloneCombatBoardRaySelect() =>
            suppressStandaloneCombatBoardRayPick && enabled;

        private bool IsAttackerCombatReady()
        {
            if (attacker == null || attacker.entityBridge == null || !attacker.entityBridge.IsValid())
                return false;
            return attacker.BoundEcsEntity.IsValid();
        }

        private void RefreshAttackRingVisualization()
        {
            var radius = CombatTargetingRange.ResolveForCaster(attacker, 0f);
            if (radius < 1e-4f)
                return;

            var center = attacker.transform.position;
            if (attackRangeUiDisc != null)
            {
                attackRangeUiDisc.SetWorldDisc(center, 1f);
                return;
            }

            if (groundIndicator != null)
                groundIndicator.PushAttackRangeCircle(center, radius, attackRangeRingColor);
        }

        private void ExitAttackTargetingMode()
        {
            if (attackRangeUiDisc != null)
                attackRangeUiDisc.SetVisible(false);

            ResolveGroundIndicator(includeInactive: false);
            if (groundIndicator != null)
                groundIndicator.HidePreset(GroundPresentationPresetKind.AttackRange);
            AttackTargetingModeActive = false;
        }

        private bool TryResolveVictimUnderCursor(out EntityBase victim)
        {
            victim = null;
            if (targetCamera == null)
                targetCamera = Camera.main;
            if (targetCamera == null)
                return false;

            var ray = targetCamera.ScreenPointToRay(Input.mousePosition);
            var mask = unitRaycastMask.value != 0 ? unitRaycastMask.value : Physics.DefaultRaycastLayers;
            if (!Physics.Raycast(ray, out var hit, rayMaxDistance, mask, QueryTriggerInteraction.Ignore))
                return false;

            var picked = hit.collider.GetComponentInParent<EntityBase>();
            if (picked == null || picked == attacker)
                return false;

            victim = picked;
            return true;
        }

        private void AttemptExecuteNormalAttackTowards(EntityBase victim)
        {
            if (!MeleeStrikeRules.TryValidateMeleeStrike(
                    attacker,
                    victim,
                    maxMeleeRangeOrZero: 0f,
                    allowDead: false,
                    out _))
                return;

            if (!CombatBoardTargetSync.SetAttackAndThreatSameTarget(attacker, victim.BoundEcsEntity.Id))
                return;

            var anim = attacker.GetComponent<UnitAnimDrv>();
            if (anim != null)
                anim.BeginNormalAttackSwing(_dispatch);
            else if (!_dispatch.TryDispatchNormalAttack(attacker, out _))
                return;

            _nextSwingTime = Time.time + swingCooldownSeconds;
        }
    }
}
