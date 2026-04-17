using UnityEngine;
using Basement.Events;
using Basement.Runtime;

namespace Basement.Events.Unity
{
    /// <summary>
    /// 事件调度器 Unity 集成。若由 <see cref="BasementPumpEcsSystem"/> 泵送，则本组件 <c>Update</c> 中不再调用调度器。
    /// </summary>
    public class EventDispatcher : MonoBehaviour
    {
        private void Awake()
        {
            // 初始化事件总线和调度器
            GameEventBus.Instance.Initialize();
            GameEventScheduler.Instance.Initialize();
        }

        private void Update()
        {
            if (BasementLoopRouting.IsEcsPumpActive)
                return;
            BasementUnityPump.PumpGameEvents();
        }

        private void OnDestroy()
        {
            // 清理事件总线
            GameEventBus.Instance.Clear();
        }

        /// <summary>
        /// 静态方法：创建事件调度器实例
        /// </summary>
        public static EventDispatcher Create()
        {
            GameObject go = new GameObject("EventDispatcher");
            return go.AddComponent<EventDispatcher>();
        }

        /// <summary>
        /// 静态方法：获取或创建事件调度器实例
        /// </summary>
        public static EventDispatcher GetOrCreate()
        {
            EventDispatcher dispatcher = FindObjectOfType<EventDispatcher>();
            if (dispatcher == null)
            {
                dispatcher = Create();
            }
            return dispatcher;
        }
    }
}
