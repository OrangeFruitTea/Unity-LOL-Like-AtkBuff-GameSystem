using Core.Entity;
using UnityEngine;

namespace Gameplay.Combat.Targeting
{
    /// <summary>
    /// 射线点敌对单位写黑板；命中 <see cref="raycastLayers"/>（若为 0 则 <see cref="Physics.DefaultRaycastLayers"/>）。<br/>
    /// （UI 点击过滤等后续按需再加。）
    /// </summary>
    public sealed class CombatBoardRaySelectTarget : MonoBehaviour
    {
        [SerializeField] private EntityBase controlledUnit;
        [SerializeField] private Camera targetCamera;
        [SerializeField] private float maxRayDistance = 500f;
        [SerializeField] private LayerMask raycastLayers;

        private void Awake()
        {
            if (controlledUnit == null)
                controlledUnit = GetComponent<EntityBase>();
            if (targetCamera == null)
                targetCamera = Camera.main;
        }

        private void Update()
        {
            if (controlledUnit == null || targetCamera == null)
                return;
            if (!Input.GetMouseButtonDown(0))
                return;

            var ray = targetCamera.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(
                    ray,
                    out var hit,
                    maxRayDistance,
                    EffectiveRaycastMask(),
                    QueryTriggerInteraction.Ignore))
                return;

            var picked = hit.collider.GetComponentInParent<EntityBase>();
            if (picked == null || picked == controlledUnit)
                return;

            if (!MeleeStrikeRules.TryValidateMeleeStrike(
                    controlledUnit,
                    picked,
                    maxMeleeRangeOrZero: 0f,
                    allowDead: false,
                    out _))
                return;

            if (!CombatBoardTargetSync.SetAttackAndThreatSameTarget(controlledUnit, picked.BoundEcsEntity.Id))
                Debug.LogWarning(
                    $"{nameof(CombatBoardRaySelectTarget)}: missing {nameof(CombatBoardLiteComponent)} on controlled unit.");
        }

        private LayerMask EffectiveRaycastMask() =>
            raycastLayers.value != 0 ? raycastLayers : (LayerMask)Physics.DefaultRaycastLayers;
    }
}
