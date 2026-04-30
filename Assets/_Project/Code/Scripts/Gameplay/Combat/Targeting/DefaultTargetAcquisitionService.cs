using Core.Entity;
using Core.ECS;
using UnityEngine;

namespace Gameplay.Combat.Targeting
{
    public sealed class DefaultTargetAcquisitionService : ITargetAcquisitionService
    {
        private const float HpEpsilon = 1e-6f;

        public TargetAcquisitionResult Acquire(in TargetAcquisitionRequest request)
        {
            var caster = request.Caster;
            if (caster == null)
                return TargetAcquisitionResult.Fail($"{nameof(TargetAcquisitionRequest.Caster)} is null");

            var casterEcs = caster.BoundEcsEntity;
            if (!casterEcs.IsValid())
                return TargetAcquisitionResult.Fail("caster has invalid BoundEcsEntity");

            switch (request.Shape)
            {
                case TargetingShapeKind.PointEntity:
                    return AcquirePointEntity(in request);
                case TargetingShapeKind.NearestInSphere:
                    return AcquireNearestInSphere(in request);
                default:
                    return TargetAcquisitionResult.Fail(
                        $"{request.Shape}: not implemented in MVP (needs P1+ per design doc).");
            }
        }

        private static TargetAcquisitionResult AcquirePointEntity(in TargetAcquisitionRequest request)
        {
            long hint = request.PrimaryEntityIdHint;
            if (hint == 0)
                return TargetAcquisitionResult.Fail($"{nameof(TargetAcquisitionRequest.PrimaryEntityIdHint)} is 0");

            var caster = request.Caster;
            var victimEcs = new EcsEntity(hint);
            float maxDist = ResolveRangeHint(request.RangeOrRadius, caster);

            if (!ValidateCommonPair(caster, victimEcs, maxDist, request.IncludeDead, out var eb, out string err))
                return TargetAcquisitionResult.Fail(err);

            var arr = new[] { eb };
            return TargetAcquisitionResult.Ok(arr, eb);
        }

        private static TargetAcquisitionResult AcquireNearestInSphere(in TargetAcquisitionRequest request)
        {
            var caster = request.Caster;
            float range = ResolveRangeHint(request.RangeOrRadius, caster);
            if (range <= 1e-6f)
                return TargetAcquisitionResult.Fail("resolved attack range is 0");

            var casterEcs = caster.BoundEcsEntity;
            if (!casterEcs.HasComponent<FactionComponent>())
                return TargetAcquisitionResult.Fail("caster has no FactionComponent");

            var myFaction = casterEcs.GetComponent<FactionComponent>().TeamId;

            if (!CombatTargetAcquire.TryPickNearestHostileInRange(casterEcs, myFaction, range, out var picked))
                return TargetAcquisitionResult.Fail("no hostile in range");

            if (!ValidateCommonPair(caster, picked, range, request.IncludeDead, out var eb, out string err2))
                return TargetAcquisitionResult.Fail(err2);

            var arr = new[] { eb };
            return TargetAcquisitionResult.Ok(arr, eb);
        }

        /// <summary>距离：施法者与目标 Transform；上限为 <paramref name="maxDist"/>。</summary>
        private static bool ValidateCommonPair(
            EntityBase caster,
            EcsEntity victimEcs,
            float maxDist,
            bool includeDead,
            out EntityBase victimHost,
            out string error)
        {
            victimHost = null;
            error = null;

            if (!victimEcs.IsValid())
            {
                error = "target ecs invalid";
                return false;
            }

            if (!EntityEcsLinkRegistry.TryGetEntityBase(victimEcs, out victimHost))
            {
                error = "target has no EntityBase in registry";
                return false;
            }

            var casterEcs = caster.BoundEcsEntity;

            if (!casterEcs.HasComponent<FactionComponent>() || !victimEcs.HasComponent<FactionComponent>())
            {
                error = "caster or target missing FactionComponent";
                return false;
            }

            var a = casterEcs.GetComponent<FactionComponent>().TeamId;
            var b = victimEcs.GetComponent<FactionComponent>().TeamId;
            if (!CombatHostility.AreHostile(a, b))
            {
                error = "targets are not hostile";
                return false;
            }

            if (!victimEcs.HasComponent<EntityDataComponent>())
            {
                error = "target missing EntityDataComponent";
                return false;
            }

            if (!includeDead)
            {
                var hp = victimEcs.GetComponent<EntityDataComponent>().GetData(EntityBaseDataCore.CrtHp);
                if (hp <= HpEpsilon)
                {
                    error = "target is dead";
                    return false;
                }
            }

            if (maxDist > 1e-6f)
            {
                if (!EntityEcsLinkRegistry.TryGetEntityBase(casterEcs, out var ego))
                {
                    error = "caster not in registry for distance";
                    return false;
                }

                float d2 = (ego.transform.position - victimHost.transform.position).sqrMagnitude;
                if (d2 > maxDist * maxDist)
                {
                    error = "target out of range";
                    return false;
                }
            }

            return true;
        }

        /// <summary><paramref name="rangeOrRadius"/> ≤0 则用 <see cref="EntityBaseData.AtkDistance"/>。</summary>
        private static float ResolveRangeHint(float rangeOrRadius, EntityBase caster)
        {
            if (rangeOrRadius > 1e-6f)
                return rangeOrRadius;

            var ecs = caster.BoundEcsEntity;
            if (!ecs.IsValid() || !ecs.HasComponent<EntityDataComponent>())
                return 0f;

            return (float)ecs.GetComponent<EntityDataComponent>().GetData(EntityBaseData.AtkDistance);
        }
    }
}
