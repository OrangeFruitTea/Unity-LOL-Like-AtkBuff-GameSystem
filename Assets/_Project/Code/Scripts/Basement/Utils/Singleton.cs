using System;
using Unity.VisualScripting;
using UnityEngine;

namespace Basement.Utils
{
    public class Singleton<T> : MonoBehaviour where T : Singleton<T>
    {
        protected static volatile T _instance;
        private static bool _isDestroyed = false;
        private static readonly object _lock = new object();
        public static T Instance
        {
            get
            {
                if (_isDestroyed)
                {
                    Debug.LogError($"Singleton<{typeof(T).Name}>不存在，无法获取实例");
                    return null;
                }

                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            var existingInstance = FindObjectOfType<T>();
                            if (existingInstance != null)
                            {
                                _instance = existingInstance;
                            }
                            else
                            {
                                var singletonObj = new GameObject($"[Singleton_{typeof(T).Name}]");
                                _instance = singletonObj.AddComponent<T>();
                                DontDestroyOnLoad(singletonObj);
                                Debug.Log($"自动创建Singleton<{typeof(T).Name}>实例");
                            }
                        }
                    }
                }
                return _instance;
            }
        }

        protected virtual void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                Debug.LogWarning($"Singleton {typeof(T).Name}已存在，销毁重复对象");
            }
            else
            {
                _instance = (T)this;
                DontDestroyOnLoad(gameObject);
                Debug.Log($"Singleton {typeof(T).Name}初始化完成");
            }
        }

        protected virtual void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
                _isDestroyed = true;
            }
        }
    }
}
