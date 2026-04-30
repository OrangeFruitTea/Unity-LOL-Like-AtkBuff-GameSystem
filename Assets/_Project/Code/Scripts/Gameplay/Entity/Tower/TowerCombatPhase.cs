namespace Core.Entity
{
    /// <summary> 防御塔普攻节拍相位；与设计文档 §5 一致。</summary>
    public enum TowerCombatPhase : byte
    {
        IdleScan = 0,
        Warning = 1,
        LockPhase = 2,
        StrikePending = 3,
        Cooldown = 4,
        Interrupted = 5,
    }
}
