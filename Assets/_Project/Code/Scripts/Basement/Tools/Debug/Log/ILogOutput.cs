namespace Basement.Logging
{
    public interface ILogOutput
    {
        void Log(string message, LogLevel level, string tag = null);
        void SetLogLevel(LogLevel level);
    }
}
