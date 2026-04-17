using System;
using System.Collections.Generic;

namespace Basement.Configuration
{
    /// <summary>
    /// 配置验证器
    /// 提供配置验证功能
    /// </summary>
    public static class ConfigurationValidator
    {
        /// <summary>
        /// 验证配置对象
        /// </summary>
        /// <param name="config">配置对象</param>
        /// <returns>验证结果</returns>
        public static bool Validate(IConfiguration config)
        {
            if (config == null)
            {
                return false;
            }

            return config.Validate();
        }

        /// <summary>
        /// 获取验证错误信息
        /// </summary>
        /// <param name="config">配置对象</param>
        /// <returns>错误信息列表</returns>
        public static List<string> GetValidationErrors(IConfiguration config)
        {
            if (config == null)
            {
                return new List<string> { "配置对象为空" };
            }

            return config.GetValidationErrors();
        }
    }
}