using Basement.Events;
using Basement.MatchTime;
using Basement.Tasks;

namespace Basement.Runtime
{
    /// <summary>
    /// 将 MatchTime、TimingTask、GameEvent 的每帧推进集中在一处，供 ECS 系统或 MonoBehaviour Dispatcher 调用。
    /// </summary>
    public static class BasementUnityPump
    {
        public static void PumpMatchTimeUpdate() => MatchTimeService.Instance.TickUpdate();

        public static void PumpMatchTimeFixed() => MatchTimeService.Instance.TickFixedUpdate();

        public static void PumpTimingTasks() => TimingTaskScheduler.Instance.Update();

        public static void PumpGameEvents() => GameEventScheduler.Instance.Update();

        /// <summary> 每帧调用一次（与 Unity <c>Update</c> 对齐）。 </summary>
        public static void PumpUpdate()
        {
            PumpMatchTimeUpdate();
            PumpTimingTasks();
            PumpGameEvents();
        }

        /// <summary> 每物理帧调用一次（与 Unity <c>FixedUpdate</c> 对齐）。 </summary>
        public static void PumpFixedUpdate() => PumpMatchTimeFixed();
    }
}
