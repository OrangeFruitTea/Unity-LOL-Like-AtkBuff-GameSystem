namespace Basement.MatchTime
{
    /// <summary>
    /// 对局时间控制：开始/结束对局与暂停（不操作 Unity <c>timeScale</c>，由项目自行决定）。
    /// </summary>
    public interface IMatchTimeControl : IMatchTime
    {
        void BeginMatch();

        void EndMatch();

        void PauseMatch();

        void ResumeMatch();
    }
}
