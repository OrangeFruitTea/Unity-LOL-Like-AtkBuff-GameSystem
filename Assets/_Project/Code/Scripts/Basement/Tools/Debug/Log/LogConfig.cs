using System;

namespace Basement.Logging
{
    [Serializable]
    public class LogConfig
    {
        public LogLevel DefaultLogLevel = LogLevel.Info;
        public string LogFilePath = null;
        public bool EnableConsoleOutput = true;
        public bool EnableFileOutput = true;
        public bool EnableDebugWindow = true;
        public int DebugWindowMaxLines = 1000;
        public bool EnableTimestamp = true;
        public bool EnableStackTrace = false;
        public int MaxLogFileSize = 10;
    }
}
