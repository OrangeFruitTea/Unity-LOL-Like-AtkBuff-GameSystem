using UnityEngine;
using Basement.Events;

namespace Basement.Events.Unity
{
    /// <summary>
    /// 事件调度器Unity集成
    /// 用于在Unity环境中管理事件调度
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
            // 更新事件调度器
            GameEventScheduler.Instance.Update();
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
