using System;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.AddressableAssets;

namespace Basement.ResourceManagement
{
    public class ResourceLoader : IResourceLoader
    {
        public T LoadSync<T>(string resourcePath) where T : UnityEngine.Object
        {
            #if UNITY_EDITOR
            // 编辑器模式下使用Resources加载，方便开发
            return Resources.Load<T>(resourcePath);
            #else
            // 发布模式下使用Addressables加载
            AsyncOperationHandle<T> handle = Addressables.LoadAssetAsync<T>(resourcePath);
            handle.WaitForCompletion();
            return handle.Result;
            #endif
        }

        public void LoadAsync<T>(string resourcePath, Action<T> onLoaded, Action<AsyncOperationHandle<T>> onProgress = null) where T : UnityEngine.Object
        {
            #if UNITY_EDITOR
            // 编辑器模式下使用Resources加载
            ResourceRequest asyncOp = Resources.LoadAsync<T>(resourcePath);
            asyncOp.completed += (op) =>
            {
                if (op is ResourceRequest request) onLoaded?.Invoke(request.asset as T);
            };
            #else
            // 发布模式下使用Addressables加载
            AsyncOperationHandle<T> handle = Addressables.LoadAssetAsync<T>(resourcePath);
            
            if (onProgress != null)
            {
                handle.Completed += (op) => onProgress?.Invoke(op);
            }
            
            handle.Completed += (op) =>
            {
                if (op.Status == AsyncOperationStatus.Succeeded)
                {
                    onLoaded?.Invoke(op.Result);
                }
                else
                {
                    Debug.LogError($"Failed to load asset: {resourcePath}, Error: {op.OperationException}");
                    onLoaded?.Invoke(null);
                }
            };
            #endif
        }

        public void Unload<T>(T resource) where T : UnityEngine.Object
        {
            if (resource == null)
                return;
            
            #if UNITY_EDITOR
            Resources.UnloadAsset(resource);
            #else
            Addressables.Release(resource);
            #endif
        }

        public void UnloadUnused()
        {
            Resources.UnloadUnusedAssets();
        }
    }
}