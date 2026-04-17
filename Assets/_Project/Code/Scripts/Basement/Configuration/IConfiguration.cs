namespace Basement.Configuration
{
    /// <summary>
    /// 配置接口
    /// 定义配置的基本属性和行为
    /// </summary>
    public interface IConfiguration
    {
        /// <summary>
        /// 配置版本
        /// </summary>
        string Version { get; set; }

        /// <summary>
        /// 配置名称
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// 配置文件路径
        /// </summary>
        string FilePath { get; set; }

        /// <summary>
        /// 最后修改时间
        /// </summary>
        System.DateTime LastModified { get; set; }

        /// <summary>
        /// 验证配置
        /// </summary>
        /// <returns>验证结果</returns>
        bool Validate();

        /// <summary>
        /// 获取验证错误信息
        /// </summary>
        /// <returns>错误信息列表</returns>
        System.Collections.Generic.List<string> GetValidationErrors();
    }
}