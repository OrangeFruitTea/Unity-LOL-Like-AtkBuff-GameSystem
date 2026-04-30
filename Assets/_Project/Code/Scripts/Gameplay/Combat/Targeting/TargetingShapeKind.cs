namespace Gameplay.Combat.Targeting
{
    /// <summary>MVP：仅 <see cref="PointEntity"/> / <see cref="NearestInSphere"/>；其余 P1+。</summary>
    public enum TargetingShapeKind
    {
        PointEntity = 0,
        NearestInSphere = 1,
        GroundCircle = 2,
        Cone = 3,
        Line = 4,
    }
}
