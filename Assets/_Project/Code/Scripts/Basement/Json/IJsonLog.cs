namespace Basement.Json
{
    /// <summary>
    /// JSON 存储/读取日志抽象，便于测试或接入统一日志模块。
    /// </summary>
    public interface IJsonLog
    {
        void LogError(string message, string tag);

        void LogWarning(string message, string tag);
    }
}
