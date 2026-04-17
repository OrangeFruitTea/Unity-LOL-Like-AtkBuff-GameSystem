using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Basement.Json
{
    /// <summary>
    /// 预置 Newtonsoft 配置：存档/运行时与只读游戏内容分离（内容表避免 TypeNameHandling.Auto）。
    /// </summary>
    public static class JsonSerializationProfiles
    {
        /// <summary> 持久化存档、编辑器工具链：允许类型名嵌入，便于多态。 </summary>
        public static JsonSerializerSettings CreateRuntimePersistenceSettings()
        {
            return new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                TypeNameHandling = TypeNameHandling.Auto,
                Converters = { new StringEnumConverter() }
            };
        }

        /// <summary> StreamingAssets 等只读配置：关闭 TypeNameHandling，降低不可信 JSON 风险；枚举按字符串。 </summary>
        public static JsonSerializerSettings CreateGameContentSettings()
        {
            return new JsonSerializerSettings
            {
                Formatting = Formatting.None,
                NullValueHandling = NullValueHandling.Ignore,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                TypeNameHandling = TypeNameHandling.None,
                Converters = { new StringEnumConverter() }
            };
        }
    }
}
