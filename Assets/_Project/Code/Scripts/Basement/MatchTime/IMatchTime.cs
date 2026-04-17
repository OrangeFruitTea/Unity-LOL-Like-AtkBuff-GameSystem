namespace Basement.MatchTime
{
    /// <summary>
    /// 对局内只读时间视图（逻辑时间受 <see cref="UnityEngine.Time.timeScale"/> 影响）。
    /// </summary>
    public interface IMatchTime
    {
        /// <summary> 本局是否已开始且未结束（<c>BeginMatch</c>～<c>EndMatch</c>）。 </summary>
        bool IsMatchActive { get; }

        /// <summary> 对局逻辑是否暂停（仅当 <see cref="IsMatchActive"/> 为真时有效）。 </summary>
        bool IsMatchPaused { get; }

        /// <summary> 自 <c>BeginMatch</c> 起累计的逻辑秒；对局未进行或暂停时不递增。 </summary>
        float MatchElapsed { get; }

        /// <summary> 当前帧逻辑增量，等同 <c>Time.deltaTime</c>。 </summary>
        float DeltaTime { get; }

        /// <summary> 最近一次固定更新步长，在 <c>FixedUpdate</c> 中与 <c>Time.deltaTime</c> 一致（含缩放）。 </summary>
        float FixedDeltaTime { get; }

        /// <summary> 全局逻辑时间快照，等同 <c>Time.time</c>。 </summary>
        float UnityScaledTime { get; }
    }
}
