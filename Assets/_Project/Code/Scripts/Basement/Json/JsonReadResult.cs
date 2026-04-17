namespace Basement.Json
{
    /// <summary>
    /// JSON 读取 / 反序列化结果，区分失败原因，便于配置表校验。
    /// </summary>
    public readonly struct JsonReadResult<T>
    {
        public bool Success { get; }
        public T Value { get; }
        public string Error { get; }

        private JsonReadResult(bool success, T value, string error)
        {
            Success = success;
            Value = value;
            Error = error;
        }

        public static JsonReadResult<T> Ok(T value) => new JsonReadResult<T>(true, value, null);

        public static JsonReadResult<T> Fail(string error) =>
            new JsonReadResult<T>(false, default, error ?? "unknown error");
    }
}
