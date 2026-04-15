#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Basement.Events;

namespace Basement.Events.Unity
{
    /// <summary>
    /// 事件调试窗口
    /// 用于在编辑器中查看和管理事件
    /// </summary>
    public class EventDebugWindow : EditorWindow
    {
        [MenuItem("Tools/Event Debug")]
        public static void ShowWindow()
        {
            GetWindow<EventDebugWindow>("事件调试器");
        }

        private void OnGUI()
        {
            GUILayout.Label("事件统计", EditorStyles.boldLabel);

            GUILayout.Label($"队列大小: {GameEventScheduler.Instance.QueueSize}");

            GUILayout.Space(10);

            if (GUILayout.Button("清空事件队列"))
            {
                GameEventScheduler.Instance.ClearQueue();
            }

            if (GUILayout.Button("清空所有订阅"))
            {
                GameEventBus.Instance.Clear();
            }

            if (GUILayout.Button("导出事件历史"))
            {
                string filePath = EditorUtility.SaveFilePanel("导出事件历史", "", "EventHistory.csv", "csv");
                if (!string.IsNullOrEmpty(filePath))
                {
                    // 这里可以实现导出逻辑
                }
            }
        }
    }
}
#endif
