using System;
using System.Threading;
using UnityEngine;
using Test;

namespace Basement.ResourceManagement.Tests
{
    /// <summary>
    /// GameObjectPool测试
    /// </summary>
    public class GameObjectPoolTest : TestBase
    {
        private GameObject _testPrefab;
        private GameObjectPool _pool;

        public GameObjectPoolTest() : base("GameObjectPoolTest") { }

        protected override void Setup()
        {
            // 创建测试预制体
            _testPrefab = new GameObject("TestPrefab");
            // 创建游戏对象池
            _pool = new GameObjectPool(_testPrefab, null, 5, 20);
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
            // 清理游戏对象池
            _pool.Clear();
            // 销毁测试预制体
            if (_testPrefab != null)
            {
                GameObject.DestroyImmediate(_testPrefab);
            }
        }

        /// <summary>
        /// 测试生成和回收游戏对象
        /// </summary>
        private void TestSpawnDespawn()
        {
            // 生成游戏对象
            GameObject obj1 = _pool.Spawn();
            AssertNotNull(obj1, "Should spawn game object");
            AssertTrue(obj1.activeSelf, "Spawned object should be active");
            AssertEqual(_pool.UsedCount, 1, "Used count should be 1 after spawn");
            
            // 生成另一个游戏对象
            GameObject obj2 = _pool.Spawn(new Vector3(1, 1, 1), Quaternion.identity);
            AssertNotNull(obj2, "Should spawn game object with position and rotation");
            AssertTrue(obj2.activeSelf, "Spawned object should be active");
            AssertEqual(_pool.UsedCount, 2, "Used count should be 2 after second spawn");
            
            // 回收游戏对象
            _pool.Despawn(obj1);
            AssertEqual(_pool.UsedCount, 1, "Used count should be 1 after despawn");
            AssertEqual(_pool.IdleCount, 1, "Idle count should be 1 after despawn");
            
            _pool.Despawn(obj2);
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
            GameObject obj;
            bool success = _pool.TrySpawn(out obj);
            AssertTrue(success, "TrySpawn should return true");
            AssertNotNull(obj, "TrySpawn should return game object");
            
            // 回收对象
            _pool.Despawn(obj);
        }

        /// <summary>
        /// 测试预加载
        /// </summary>
        private void TestPreload()
        {
            // 清空池
            _pool.Clear();
            AssertEqual(_pool.IdleCount, 0, "Idle count should be 0 after clear");
            
            // 预加载10个对象
            _pool.Preload(10);
            AssertEqual(_pool.IdleCount, 10, "Idle count should be 10 after preload");
            AssertEqual(_pool.UsedCount, 0, "Used count should be 0 after preload");
        }

        /// <summary>
        /// 测试清空池
        /// </summary>
        private void TestClear()
        {
            // 生成一些对象
            GameObject obj1 = _pool.Spawn();
            GameObject obj2 = _pool.Spawn();
            
            // 回收一个对象
            _pool.Despawn(obj1);
            
            // 清空池
            _pool.Clear();
            
            AssertEqual(_pool.IdleCount, 0, "Idle count should be 0 after clear");
            AssertEqual(_pool.UsedCount, 0, "Used count should be 0 after clear");
            
            // 回收剩余对象（应该不会有问题）
            _pool.Despawn(obj2);
        }

        /// <summary>
        /// 测试IReusable接口
        /// </summary>
        private void TestIReusable()
        {
            // 为预制体添加ReusableComponent
            ReusableComponent component = _testPrefab.AddComponent<ReusableComponent>();
            
            // 清空池
            _pool.Clear();
            
            // 生成对象
            GameObject obj = _pool.Spawn();
            ReusableComponent objComponent = obj.GetComponent<ReusableComponent>();
            AssertNotNull(objComponent, "Object should have ReusableComponent");
            AssertTrue(objComponent.IsSpawned, "IsSpawned should be true after spawn");
            
            // 回收对象
            _pool.Despawn(obj);
            AssertFalse(objComponent.IsSpawned, "IsSpawned should be false after despawn");
        }

        /// <summary>
        /// 可重用组件
        /// </summary>
        private class ReusableComponent : MonoBehaviour, IReusable
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
