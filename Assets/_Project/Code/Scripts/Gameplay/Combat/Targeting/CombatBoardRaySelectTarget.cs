using Core.Entity;
using Core.ECS;
using UnityEngine;

namespace Gameplay.Combat.Targeting
{
    /// <summary>MVP：鼠标射线点选敌对单位写入操控者 <see cref="CombatBoardLiteComponent.AttackTargetEntityId"/>。</summary>
    public sealed class CombatBoardRaySelectTarget : MonoBehaviour
    {
        [SerializeField] private EntityBase controlledUnit;
        [SerializeField] private Camera targetCamera;

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
            if (!Physics.Raycast(ray, out var hit, 500f))
                return;

            var picked = hit.collider.GetComponentInParent<EntityBase>();
            if (picked == null || picked == controlledUnit)
                return;

            var casterEcs = controlledUnit.BoundEcsEntity;
            var pickEcs = picked.BoundEcsEntity;

            if (!casterEcs.IsValid() ||
                !pickEcs.IsValid() ||
                !casterEcs.HasComponent<FactionComponent>() ||
                !pickEcs.HasComponent<FactionComponent>())
                return;

            if (!CombatHostility.AreHostile(
                    casterEcs.GetComponent<FactionComponent>().TeamId,
                    pickEcs.GetComponent<FactionComponent>().TeamId))
                return;

            if (!CombatBoardTargetSync.SetPrimaryAttackTarget(controlledUnit, pickEcs.Id))
                Debug.LogWarning(
                    $"{nameof(CombatBoardRaySelectTarget)}: controlled unit missing CombatBoardLiteComponent.");
        }
    }
}
