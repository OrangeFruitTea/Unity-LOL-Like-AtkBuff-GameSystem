namespace Basement.Runtime
{
    /// <summary>
    /// 标记当前是否由 ECS 系统泵送 Basement 的每帧逻辑，供 MonoBehaviour Dispatcher 判断是否跳过 <c>Update</c>。
    /// </summary>
    public static class BasementLoopRouting
    {
        public static bool IsEcsPumpActive { get; private set; }

        internal static void MarkEcsPumpActive() => IsEcsPumpActive = true;

        internal static void ResetForTests()
        {
            IsEcsPumpActive = false;
        }
    }
}
