using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Basement.Threading
{
    /// <summary>
    /// 协程管理器
    /// </summary>
    public class CoroutineManager : MonoBehaviour
    {
        private readonly Dictionary<object, List<Coroutine>> _coroutines = new Dictionary<object, List<Coroutine>>();
        private readonly object _lock = new object();
        

        /// <summary>
        /// 启动协程
        /// </summary>
        public Coroutine StartCoroutine(IEnumerator enumerator, object owner = null)
        {
            Coroutine coroutine = base.StartCoroutine(enumerator);
            
            if (owner != null)
            {
                lock (_lock)
                {
                    if (!_coroutines.TryGetValue(owner, out var list))
                    {
                        list = new List<Coroutine>();
                        _coroutines[owner] = list;
                    }
                    list.Add(coroutine);
                }
            }
            
            return coroutine;
        }

        /// <summary>
        /// 停止协程
        /// </summary>
        public void StopCoroutine(Coroutine coroutine)
        {
            if (coroutine != null)
            {
                base.StopCoroutine(coroutine);
            }
        }

        /// <summary>
        /// 停止所有协程
        /// </summary>
        public void StopAllCoroutines(object owner = null)
        {
            if (owner != null)
            {
                lock (_lock)
                {
                    if (_coroutines.TryGetValue(owner, out var list))
                    {
                        foreach (var coroutine in list)
                        {
                            base.StopCoroutine(coroutine);
                        }
                        _coroutines.Remove(owner);
                    }
                }
            }
            else
            {
                base.StopAllCoroutines();
                lock (_lock)
                {
                    _coroutines.Clear();
                }
            }
        }

        /// <summary>
        /// 等待指定时间后执行
        /// </summary>
        public void WaitForSeconds(float seconds, Action onComplete, object owner = null)
        {
            StartCoroutine(WaitForSecondsCoroutine(seconds, onComplete), owner);
        }

        private IEnumerator WaitForSecondsCoroutine(float seconds, Action onComplete)
        {
            yield return new WaitForSeconds(seconds);
            onComplete?.Invoke();
        }

        /// <summary>
        /// 等待指定帧数后执行
        /// </summary>
        public void WaitForFrames(int frames, Action onComplete, object owner = null)
        {
            StartCoroutine(WaitForFramesCoroutine(frames, onComplete), owner);
        }

        private IEnumerator WaitForFramesCoroutine(int frames, Action onComplete)
        {
            for (int i = 0; i < frames; i++)
            {
                yield return null;
            }
            onComplete?.Invoke();
        }

        /// <summary>
        /// 等待直到条件满足
        /// </summary>
        public void WaitUntil(Func<bool> condition, Action onComplete, object owner = null)
        {
            StartCoroutine(WaitUntilCoroutine(condition, onComplete), owner);
        }

        private IEnumerator WaitUntilCoroutine(Func<bool> condition, Action onComplete)
        {
            yield return new WaitUntil(condition);
            onComplete?.Invoke();
        }

        /// <summary>
        /// 更新方法
        /// 用于驱动协程的执行
        /// </summary>
        private void Update()
        {
            // Unity会自动调用此方法来驱动协程
            // 无需添加额外逻辑
        }
    }
}
