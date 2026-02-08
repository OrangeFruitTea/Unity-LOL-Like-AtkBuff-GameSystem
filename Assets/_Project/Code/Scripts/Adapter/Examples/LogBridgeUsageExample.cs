using Adapter;
using Adapter.Bridge;
using Adapter.Interfaces;

namespace Adapter.Examples
{
    public class LogBridgeUsageExample
    {
        public void BasicUsage()
        {
            var logger = LogBridge.Instance;

            logger.Debug("这是一条调试信息", "Example");
            logger.Info("这是一条普通信息", "Example");
            logger.Warning("这是一条警告信息", "Example");
            logger.Error("这是一条错误信息", "Example");
            logger.Fatal("这是一条致命错误信息", "Example");
        }

        public void ExceptionHandling()
        {
            try
            {
                throw new System.Exception("测试异常");
            }
            catch (System.Exception ex)
            {
                LogBridge.Instance.Exception(ex, "Example");
            }
        }

        public void LogLevelControl()
        {
            var logger = LogBridge.Instance;

            logger.SetLogLevel(LogLevel.Warning);

            logger.Debug("这条Debug日志不会显示", "Example");
            logger.Info("这条Info日志不会显示", "Example");
            logger.Warning("这条Warning日志会显示", "Example");
            logger.Error("这条Error日志会显示", "Example");

            LogLevel currentLevel = logger.GetLogLevel();
            UnityEngine.Debug.Log($"当前日志级别: {currentLevel}");
        }

        public void AdapterPatternUsage()
        {
            ILogAdapter logger = LogBridge.Instance;
            logger.Info("通过ILogAdapter接口调用", "Example");
        }
    }
}
