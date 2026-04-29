namespace Gameplay.Skill.Config
{
    /// <summary>
    /// Buff 施加触发语义（与设计文档 5.3 对齐）。
    /// </summary>
    public enum BuffApplicationTriggerKind
    {
        Immediate = 0,
        AfterDelay = 1,
        OnCondition = 2,
        OnEvent = 3
    }
}
