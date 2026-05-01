using Core.Entity;
using Core.ECS;

namespace Gameplay.Combat.Targeting
{
    public sealed class DefaultTargetAcquisitionService : ITargetAcquisitionService
    {
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
            float maxDist = CombatTargetingRange.ResolveForCaster(caster, request.RangeOrRadius);

            if (!EntityEcsLinkRegistry.TryGetEntityBase(victimEcs, out var victimHost))
                return TargetAcquisitionResult.Fail("hint target has no EntityBase in registry");

            if (!MeleeStrikeRules.TryValidateMeleeStrike(
                    caster,
                    victimHost,
                    maxDist,
                    request.IncludeDead,
                    out string err))
                return TargetAcquisitionResult.Fail(err);

            var arr = new[] { victimHost };
            return TargetAcquisitionResult.Ok(arr, victimHost);
        }

        private static TargetAcquisitionResult AcquireNearestInSphere(in TargetAcquisitionRequest request)
        {
            if (!HostileTargetPicker.TryPickNearestValidatedHostile(
                    request.Caster,
                    request.RangeOrRadius,
                    request.IncludeDead,
                    out var hostile,
                    out string error))
                return TargetAcquisitionResult.Fail(error);

            var arr = new[] { hostile };
            return TargetAcquisitionResult.Ok(arr, hostile);
        }
    }
}
