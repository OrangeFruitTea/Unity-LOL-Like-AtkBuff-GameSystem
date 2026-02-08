using UnityEngine;

namespace Adapter.Bridge
{
    public abstract class SingletonAdapter<T> : MonoBehaviour where T : SingletonAdapter<T>
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
                    Debug.LogError($"SingletonAdapter<{typeof(T).Name}>不存在，无法获取实例");
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
            }
            else
            {
                _instance = (T)this;
                DontDestroyOnLoad(gameObject);
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
