#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Basement.Tasks;

namespace Basement.Tasks.Unity
{
    /// <summary>
    /// 任务调试窗口
    /// 用于在编辑器中查看和管理任务
    /// </summary>
    public class TaskDebugWindow : EditorWindow
    {
        [MenuItem("Tools/Task Debug")]
        public static void ShowWindow()
        {
            GetWindow<TaskDebugWindow>("任务调试器");
        }

        private void OnGUI()
        {
            GUILayout.Label("任务统计", EditorStyles.boldLabel);

            var stats = TimingTaskManager.Instance.GetStatistics();
            GUILayout.Label($"总任务数: {stats.TotalCount}");
            GUILayout.Label($"等待中: {stats.ReadyCount}");
            GUILayout.Label($"执行中: {stats.RunningCount}");
            GUILayout.Label($"已完成: {stats.CompletedCount}");

            GUILayout.Space(10);

            if (GUILayout.Button("清空已完成任务"))
            {
                TimingTaskManager.Instance.ClearCompletedTasks();
            }

            if (GUILayout.Button("清空所有任务"))
            {
                TimingTaskManager.Instance.ClearAllTasks();
            }

            GUILayout.Space(20);
            GUILayout.Label("活跃任务数量: " + TimingTaskScheduler.Instance.ActiveTaskCount);
        }
    }
}
#endif
