using System;
using System.Collections;
using System.Threading;
using UnityEngine;
using Test;

namespace Basement.Threading.Tests
{
    /// <summary>
    /// CoroutineManager测试
    /// </summary>
    public class CoroutineManagerTest : TestBase
    {
        private GameObject _testObject;
        private CoroutineManager _coroutineManager;

        public CoroutineManagerTest() : base("CoroutineManagerTest") { }

        protected override void Setup()
        {
            // 创建测试对象并添加CoroutineManager组件
            _testObject = new GameObject("TestObject");
            _coroutineManager = _testObject.AddComponent<CoroutineManager>();
        }

        protected override void Execute()
        {
            TestStartCoroutine();
            TestWaitForSeconds();
            TestWaitForFrames();
            TestWaitUntil();
            TestStopCoroutine();
            TestStopAllCoroutines();
            TestStopAllCoroutinesWithOwner();
        }

        protected override void Teardown()
        {
            // 清理测试对象
            if (_testObject != null)
            {
                GameObject.DestroyImmediate(_testObject);
            }
        }

        /// <summary>
        /// 测试启动协程
        /// </summary>
        private void TestStartCoroutine()
        {
            bool executed = false;
            IEnumerator TestCoroutine()
            {
                executed = true;
                yield break;
            }

            _coroutineManager.StartCoroutine(TestCoroutine());
            
            // 手动执行协程
            // 注意：在Unity编辑器中，协程会在下一帧执行，这里我们使用Thread.Sleep模拟
            Thread.Sleep(100);
            
            AssertTrue(executed, "Coroutine should be executed");
        }

        /// <summary>
        /// 测试等待指定时间
        /// </summary>
        private void TestWaitForSeconds()
        {
            bool executed = false;
            DateTime startTime = DateTime.Now;

            _coroutineManager.WaitForSeconds(0.1f, () => executed = true);
            
            // 等待足够的时间
            Thread.Sleep(200);
            
            AssertTrue(executed, "WaitForSeconds should execute callback");
            TimeSpan elapsed = DateTime.Now - startTime;
            AssertTrue(elapsed.TotalMilliseconds >= 100, "WaitForSeconds should wait at least specified time");
        }

        /// <summary>
        /// 测试等待指定帧数
        /// </summary>
        private void TestWaitForFrames()
        {
            bool executed = false;

            _coroutineManager.WaitForFrames(2, () => executed = true);
            
            // 手动执行协程
            // 模拟两帧
            Thread.Sleep(100);
            
            AssertTrue(executed, "WaitForFrames should execute callback");
        }

        /// <summary>
        /// 测试等待直到条件满足
        /// </summary>
        private void TestWaitUntil()
        {
            bool executed = false;
            bool conditionMet = false;

            _coroutineManager.WaitUntil(() => conditionMet, () => executed = true);
            
            // 等待一段时间，条件未满足
            Thread.Sleep(100);
            AssertFalse(executed, "WaitUntil should not execute before condition is met");
            
            // 设置条件满足
            conditionMet = true;
            
            // 等待协程执行
            Thread.Sleep(100);
            AssertTrue(executed, "WaitUntil should execute after condition is met");
        }

        /// <summary>
        /// 测试停止协程
        /// </summary>
        private void TestStopCoroutine()
        {
            bool executed = false;
            Coroutine coroutine = null;

            IEnumerator TestCoroutine()
            {
                yield return new WaitForSeconds(0.5f);
                executed = true;
            }

            coroutine = _coroutineManager.StartCoroutine(TestCoroutine());
            _coroutineManager.StopCoroutine(coroutine);
            
            // 等待足够的时间
            Thread.Sleep(600);
            
            AssertFalse(executed, "Coroutine should be stopped");
        }

        /// <summary>
        /// 测试停止所有协程
        /// </summary>
        private void TestStopAllCoroutines()
        {
            bool executed1 = false;
            bool executed2 = false;

            IEnumerator TestCoroutine1()
            {
                yield return new WaitForSeconds(0.2f);
                executed1 = true;
            }

            IEnumerator TestCoroutine2()
            {
                yield return new WaitForSeconds(0.2f);
                executed2 = true;
            }

            _coroutineManager.StartCoroutine(TestCoroutine1());
            _coroutineManager.StartCoroutine(TestCoroutine2());
            _coroutineManager.StopAllCoroutines();
            
            // 等待足够的时间
            Thread.Sleep(300);
            
            AssertFalse(executed1, "All coroutines should be stopped");
            AssertFalse(executed2, "All coroutines should be stopped");
        }

        /// <summary>
        /// 测试停止指定所有者的所有协程
        /// </summary>
        private void TestStopAllCoroutinesWithOwner()
        {
            bool executed1 = false;
            bool executed2 = false;
            object owner1 = new object();
            object owner2 = new object();

            IEnumerator TestCoroutine1()
            {
                yield return new WaitForSeconds(0.2f);
                executed1 = true;
            }

            IEnumerator TestCoroutine2()
            {
                yield return new WaitForSeconds(0.2f);
                executed2 = true;
            }

            _coroutineManager.StartCoroutine(TestCoroutine1(), owner1);
            _coroutineManager.StartCoroutine(TestCoroutine2(), owner2);
            _coroutineManager.StopAllCoroutines(owner1);
            
            // 等待足够的时间
            Thread.Sleep(300);
            
            AssertFalse(executed1, "Coroutines with specified owner should be stopped");
            AssertTrue(executed2, "Coroutines with different owner should continue");
        }
    }
}