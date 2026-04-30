namespace Core.Entity
{
    /// <summary> 毕设简化：蓝/红对立；中立与任意非中立互相可敌（野区）。同阵营不互打。 </summary>
    public static class CombatHostility
    {
        public static bool AreHostile(FactionTeamId a, FactionTeamId b)
        {
            if (a == b)
                return false;
            if (a == FactionTeamId.Neutral || b == FactionTeamId.Neutral)
                return true;
            return a != b;
        }
    }
}
