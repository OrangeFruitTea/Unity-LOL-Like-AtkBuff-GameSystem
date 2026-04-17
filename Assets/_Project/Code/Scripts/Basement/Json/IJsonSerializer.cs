using System;

namespace Basement.Json
{
    public interface IJsonSerializer
    {
        string Serialize<T>(T obj);
        T Deserialize<T>(string json);
        object Deserialize(string json, Type type);
        byte[] SerializeToBytes<T>(T obj);
        T DeserializeFromBytes<T>(byte[] bytes);

        /// <summary> 不输出日志；失败时 <paramref name="error"/> 含原因。 </summary>
        bool TryDeserialize<T>(string json, out T value, out string error);

        bool TryDeserialize(string json, Type type, out object value, out string error);
    }
}
