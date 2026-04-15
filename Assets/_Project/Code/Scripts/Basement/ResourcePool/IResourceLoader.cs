using System;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Basement.ResourceManagement
{
    public interface IResourceLoader
    {
        /// <summary>
        /// 同步加载资源
        /// </summary>
        T LoadSync<T>(string resourcePath) where T : UnityEngine.Object;
        
        /// <summary>
        /// 异步加载资源
        /// </summary>
        void LoadAsync<T>(string resourcePath, Action<T> onLoaded, Action<AsyncOperationHandle<T>> onProgress = null) where T : UnityEngine.Object;
        
        /// <summary>
        /// 卸载资源
        /// </summary>
        void Unload<T>(T resource) where T : UnityEngine.Object;
        
        /// <summary>
        /// 卸载所有未使用的资源
        /// </summary>
        void UnloadUnused();
    }
}