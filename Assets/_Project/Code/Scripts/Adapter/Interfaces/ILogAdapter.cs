using Adapter;

namespace Adapter.Interfaces
{
    public interface ILogAdapter
    {
        void Debug(string message, string tag = null);
        void Info(string message, string tag = null);
        void Warning(string message, string tag = null);
        void Error(string message, string tag = null);
        void Fatal(string message, string tag = null);
        void Exception(System.Exception exception, string tag = null);

        void SetLogLevel(LogLevel level);
        LogLevel GetLogLevel();
    }
}
