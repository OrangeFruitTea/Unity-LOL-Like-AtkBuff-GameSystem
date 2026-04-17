namespace Basement.Configuration
{
    /// <summary>
    /// 配置解析器接口
    /// 定义配置解析的标准操作
    /// </summary>
    public interface IConfigurationParser
    {
        /// <summary>
        /// 支持的文件扩展名
        /// </summary>
        string[] SupportedExtensions { get; }

        /// <summary>
        /// 解析配置文件
        /// </summary>
        /// <typeparam name="T">配置类型</typeparam>
        /// <param name="filePath">文件路径</param>
        /// <returns>解析的配置对象</returns>
        T Parse<T>(string filePath) where T : IConfiguration, new();

        /// <summary>
        /// 从字符串解析配置
        /// </summary>
        /// <typeparam name="T">配置类型</typeparam>
        /// <param name="content">配置内容</param>
        /// <returns>解析的配置对象</returns>
        T ParseFromString<T>(string content) where T : IConfiguration, new();

        /// <summary>
        /// 保存配置到文件
        /// </summary>
        /// <typeparam name="T">配置类型</typeparam>
        /// <param name="config">配置对象</param>
        /// <param name="filePath">文件路径</param>
        void Save<T>(T config, string filePath) where T : IConfiguration;

        /// <summary>
        /// 将配置转换为字符串
        /// </summary>
        /// <typeparam name="T">配置类型</typeparam>
        /// <param name="config">配置对象</param>
        /// <returns>配置字符串</returns>
        string ToString<T>(T config) where T : IConfiguration;
    }
}