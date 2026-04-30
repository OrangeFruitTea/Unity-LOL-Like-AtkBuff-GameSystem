using Core.Entity;
using UnityEngine;

namespace Gameplay.Combat.Targeting
{
    /// <summary>MVP 仅用 <see cref="TargetingShapeKind.PointEntity"/> / <see cref="NearestInSphere"/>。</summary>
    public readonly struct TargetAcquisitionRequest
    {
        public readonly TargetingShapeKind Shape;
        public readonly EntityBase Caster;
        public readonly long PrimaryEntityIdHint;
        public readonly Vector3 WorldOrigin;
        public readonly Vector3 WorldDirection;
        public readonly float RangeOrRadius;
        public readonly float SecondaryParam;
        public readonly bool IncludeDead;

        public TargetAcquisitionRequest(
            TargetingShapeKind shape,
            EntityBase caster,
            long primaryEntityIdHint = 0,
            Vector3 worldOrigin = default,
            Vector3 worldDirection = default,
            float rangeOrRadius = 0f,
            float secondaryParam = 0f,
            bool includeDead = false)
        {
            Shape = shape;
            Caster = caster;
            PrimaryEntityIdHint = primaryEntityIdHint;
            WorldOrigin = worldOrigin;
            WorldDirection = worldDirection;
            RangeOrRadius = rangeOrRadius;
            SecondaryParam = secondaryParam;
            IncludeDead = includeDead;
        }
    }
}
