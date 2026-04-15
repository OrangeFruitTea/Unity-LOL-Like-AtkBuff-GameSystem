using System;
using System.Threading;
using Test;

namespace Basement.ResourceManagement.Tests
{
    /// <summary>
    /// GenericResourcePool测试
    /// </summary>
    public class GenericResourcePoolTest : TestBase
    {
        private GenericResourcePool<TestResource> _pool;

        public GenericResourcePoolTest() : base("GenericResourcePoolTest") { }

        protected override void Setup()
        {
            // 创建通用资源池
            _pool = new GenericResourcePool<TestResource>(
                () => new TestResource(),
                (res) => res.OnSpawn(),
                (res) => res.OnDespawn(),
                5, 
                20
            );
        }

        protected override void Execute()
        {
            TestSpawnDespawn();
            TestTrySpawn();
            TestPreload();
            TestClear();
            TestIReusable();
        }

        protected override void Teardown()
        {
            // 清理资源池
            _pool.Clear();
        }

        /// <summary>
        /// 测试生成和回收资源
        /// </summary>
        private void TestSpawnDespawn()
        {
            // 生成资源
            TestResource res1 = _pool.Spawn();
            AssertNotNull(res1, "Should spawn resource");
            AssertTrue(res1.IsSpawned, "Spawned resource should be marked as spawned");
            AssertEqual(_pool.UsedCount, 1, "Used count should be 1 after spawn");
            
            // 生成另一个资源
            TestResource res2 = _pool.Spawn();
            AssertNotNull(res2, "Should spawn another resource");
            AssertTrue(res2.IsSpawned, "Spawned resource should be marked as spawned");
            AssertEqual(_pool.UsedCount, 2, "Used count should be 2 after second spawn");
            
            // 回收资源
            _pool.Despawn(res1);
            AssertEqual(_pool.UsedCount, 1, "Used count should be 1 after despawn");
            AssertEqual(_pool.IdleCount, 1, "Idle count should be 1 after despawn");
            
            _pool.Despawn(res2);
            AssertEqual(_pool.UsedCount, 0, "Used count should be 0 after all despawn");
            AssertEqual(_pool.IdleCount, 2, "Idle count should be 2 after all despawn");
            
            // 测试回收空对象
            _pool.Despawn(null);
            // 应该不会抛出异常
        }

        /// <summary>
        /// 测试TrySpawn方法
        /// </summary>
        private void TestTrySpawn()
        {
            // 测试TrySpawn
            TestResource res;
            bool success = _pool.TrySpawn(out res);
            AssertTrue(success, "TrySpawn should return true");
            AssertNotNull(res, "TrySpawn should return resource");
            
            // 回收资源
            _pool.Despawn(res);
        }

        /// <summary>
        /// 测试预加载
        /// </summary>
        private void TestPreload()
        {
            // 清空池
            _pool.Clear();
            AssertEqual(_pool.IdleCount, 0, "Idle count should be 0 after clear");
            
            // 预加载10个资源
            _pool.Preload(10);
            AssertEqual(_pool.IdleCount, 10, "Idle count should be 10 after preload");
            AssertEqual(_pool.UsedCount, 0, "Used count should be 0 after preload");
        }

        /// <summary>
        /// 测试清空池
        /// </summary>
        private void TestClear()
        {
            // 生成一些资源
            TestResource res1 = _pool.Spawn();
            TestResource res2 = _pool.Spawn();
            
            // 回收一个资源
            _pool.Despawn(res1);
            
            // 清空池
            _pool.Clear();
            
            AssertEqual(_pool.IdleCount, 0, "Idle count should be 0 after clear");
            AssertEqual(_pool.UsedCount, 0, "Used count should be 0 after clear");
            
            // 回收剩余资源（应该不会有问题）
            _pool.Despawn(res2);
        }

        /// <summary>
        /// 测试IReusable接口
        /// </summary>
        private void TestIReusable()
        {
            // 清空池
            _pool.Clear();
            
            // 生成资源
            TestResource res = _pool.Spawn();
            AssertTrue(res.IsSpawned, "IsSpawned should be true after spawn");
            
            // 回收资源
            _pool.Despawn(res);
            AssertFalse(res.IsSpawned, "IsSpawned should be false after despawn");
        }

        /// <summary>
        /// 测试资源类
        /// </summary>
        private class TestResource : IReusable
        {
            public bool IsSpawned { get; private set; }

            public void OnSpawn()
            {
                IsSpawned = true;
            }

            public void OnDespawn()
            {
                IsSpawned = false;
            }
        }
    }
}
