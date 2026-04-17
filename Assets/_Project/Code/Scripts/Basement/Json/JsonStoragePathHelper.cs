using System.IO;

namespace Basement.Json
{
    /// <summary>
    /// 将逻辑 key 转为单层文件名：路径分隔符与冒号替换为下划线；
    /// <see cref="GetAllKeys"/> 侧用下划线还原为斜杠，故 key 中若含裸下划线会与目录语义混淆，宜避免或改用专用编码。
    /// </summary>
    internal static class JsonStoragePathHelper
    {
        internal static string ToSafeKeyFileName(string key)
        {
            if (string.IsNullOrEmpty(key))
                return "_empty";
            return key
                .Replace("/", "_")
                .Replace("\\", "_")
                .Replace(":", "_");
        }

        internal static string GetFilePath(string rootPath, string key, string fileExtension)
        {
            string safeKey = ToSafeKeyFileName(key);
            return Path.Combine(rootPath, $"{safeKey}{fileExtension}");
        }
    }
}
