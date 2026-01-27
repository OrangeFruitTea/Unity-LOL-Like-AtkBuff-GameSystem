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
    }
}
