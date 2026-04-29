namespace Gameplay.Skill.Buff
{
    /// <summary>
    /// 运行时修饰施加参数（装备/天赋等）；设计文档 7.2 扩展点。
    /// </summary>
    public interface IBuffApplicationModifier
    {
        void Modify(BuffApplyRequest request);
    }
}
