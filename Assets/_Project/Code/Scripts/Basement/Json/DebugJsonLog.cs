using UnityEngine;

namespace Basement.Json
{
    public sealed class DebugJsonLog : IJsonLog
    {
        public void LogError(string message, string tag)
        {
            Debug.LogError(string.IsNullOrEmpty(tag) ? message : $"[{tag}] {message}");
        }

        public void LogWarning(string message, string tag)
        {
            Debug.LogWarning(string.IsNullOrEmpty(tag) ? message : $"[{tag}] {message}");
        }
    }
}
