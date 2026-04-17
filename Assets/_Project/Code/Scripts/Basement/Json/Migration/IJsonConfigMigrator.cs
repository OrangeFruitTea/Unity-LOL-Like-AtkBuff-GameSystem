namespace Basement.Json.Migration
{
    /// <summary>
    /// 配置 JSON 单步迁移（例如 schemaVersion 1 → 2）。
    /// </summary>
    public interface IJsonConfigMigrator
    {
        int FromVersion { get; }
        int ToVersion { get; }

        /// <summary> 输入为 FromVersion 语义的 JSON 文本，输出为 ToVersion 语义。 </summary>
        string Migrate(string json);
    }
}
