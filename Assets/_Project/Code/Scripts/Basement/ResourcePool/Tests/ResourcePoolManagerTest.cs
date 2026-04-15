using System;
using System.Threading;
using UnityEngine;
using Test;

namespace Basement.ResourceManagement.Tests
{
    /// <summary>
    /// ResourcePoolManager测试
    /// </summary>
    public class ResourcePoolManagerTest : TestBase
    {
        private GameObject _testPrefab;

        public ResourcePoolManagerTest() : base("ResourcePoolManagerTest") { }

        protected override void Setup()
        {
            // 创建测试预制体
            _testPrefab = new GameObject("TestPrefab");
            // 初始化资源池管理器
            ResourcePoolManager.Instance.Initialize();
        }

        protected override void Execute()
        {
            TestCreateGameObjectPool();
            TestSpawnDespawnGameObject();
            TestCreateGenericPool();
            TestSpawnDespawnGeneric();
            TestPreloadGameObjectPool();
            TestPreloadGenericPool();
            TestClearAllPools();
        }

        protected override void Teardown()
        {
            // 清理所有资源池
            ResourcePoolManager.Instance.ClearAllPools();
            // 销毁测试预制体
            if (_testPrefab != null)
            {
                GameObject.DestroyImmediate(_testPrefab);
            }
        }

        /// <summary>
        /// 测试创建游戏对象池
        /// </summary>
        private void TestCreateGameObjectPool()
        {
            string poolKey = "TestGameObjectPool";
            
            // 创建游戏对象池
            GameObjectPool pool = ResourcePoolManager.Instance.CreateGameObjectPool(poolKey, _testPrefab, 5, 20);
            
            AssertNotNull(pool, "GameObject pool should be created");
            
            // 测试重复创建同一个池
            GameObjectPool samePool = ResourcePoolManager.Instance.CreateGameObjectPool(poolKey, _testPrefab, 5, 20);
            AssertNotNull(samePool, "Should return existing pool when creating with same key");
            AssertEqual(pool, samePool, "Should return the same pool instance when creating with same key");
        }

        /// <summary>
        /// 测试生成和回收游戏对象
        /// </summary>
        private void TestSpawnDespawnGameObject()
        {
            string poolKey = "TestSpawnDespawn";
            
            // 创建游戏对象池
            ResourcePoolManager.Instance.CreateGameObjectPool(poolKey, _testPrefab, 5, 20);
            
            // 生成游戏对象
            GameObject obj1 = ResourcePoolManager.Instance.SpawnGameObject(poolKey);
            AssertNotNull(obj1, "Should spawn game object");
            
            // 生成另一个游戏对象
            GameObject obj2 = ResourcePoolManager.Instance.SpawnGameObject(poolKey, new Vector3(1, 1, 1), Quaternion.identity);
            AssertNotNull(obj2, "Should spawn game object with position and rotation");
            
            // 回收游戏对象
            ResourcePoolManager.Instance.DespawnGameObject(poolKey, obj1);
            ResourcePoolManager.Instance.DespawnGameObject(poolKey, obj2);
            
            // 测试生成不存在的池
            GameObject nonExistentObj = ResourcePoolManager.Instance.SpawnGameObject("NonExistentPool");
            AssertNull(nonExistentObj, "Should return null when spawning from non-existent pool");
        }

        /// <summary>
        /// 测试创建通用资源池
        /// </summary>
        private void TestCreateGenericPool()
        {
            string poolKey = "TestGenericPool";
            
            // 创建通用资源池
            GenericResourcePool<TestResource> pool = ResourcePoolManager.Instance.CreateGenericPool<TestResource>(
                poolKey, 
                () => new TestResource(),
                (res) => res.OnSpawn(),
                (res) => res.OnDespawn(),
                5, 
                20
            );
            
            AssertNotNull(pool, "Generic pool should be created");
        }

        /// <summary>
        /// 测试生成和回收通用资源
        /// </summary>
        private void TestSpawnDespawnGeneric()
        {
            string poolKey = "TestGenericSpawnDespawn";
            
            // 创建通用资源池
            ResourcePoolManager.Instance.CreateGenericPool<TestResource>(
                poolKey, 
                () => new TestResource(),
                (res) => res.OnSpawn(),
                (res) => res.OnDespawn(),
                5, 
                20
            );
            
            // 生成资源
            TestResource res1 = ResourcePoolManager.Instance.SpawnGeneric<TestResource>(poolKey);
            AssertNotNull(res1, "Should spawn generic resource");
            
            // 生成另一个资源
            TestResource res2 = ResourcePoolManager.Instance.SpawnGeneric<TestResource>(poolKey);
            AssertNotNull(res2, "Should spawn another generic resource");
            
            // 回收资源
            ResourcePoolManager.Instance.DespawnGeneric<TestResource>(poolKey, res1);
            ResourcePoolManager.Instance.DespawnGeneric<TestResource>(poolKey, res2);
            
            // 测试生成不存在的池
            TestResource nonExistentRes = ResourcePoolManager.Instance.SpawnGeneric<TestResource>("NonExistentPool");
            AssertNull(nonExistentRes, "Should return null when spawning from non-existent pool");
        }

        /// <summary>
        /// 测试预加载游戏对象池
        /// </summary>
        private void TestPreloadGameObjectPool()
        {
            string poolKey = "TestPreloadGameObject";
            
            // 创建游戏对象池
            GameObjectPool pool = ResourcePoolManager.Instance.CreateGameObjectPool(poolKey, _testPrefab, 0, 20);
            
            // 预加载5个游戏对象
            ResourcePoolManager.Instance.PreloadGameObjectPool(poolKey, 5);
            
            // 验证预加载是否成功
            AssertTrue(pool.IdleCount >= 5, "Should preload at least 5 game objects");
        }

        /// <summary>
        /// 测试预加载通用资源池
        /// </summary>
        private void TestPreloadGenericPool()
        {
            string poolKey = "TestPreloadGeneric";
            
            // 创建通用资源池
            GenericResourcePool<TestResource> pool = ResourcePoolManager.Instance.CreateGenericPool<TestResource>(
                poolKey, 
                () => new TestResource(),
                null,
                null,
                0, 
                20
            );
            
            // 预加载5个资源
            ResourcePoolManager.Instance.PreloadGenericPool<TestResource>(poolKey, 5);
            
            // 验证预加载是否成功
            AssertTrue(pool.IdleCount >= 5, "Should preload at least 5 generic resources");
        }

        /// <summary>
        /// 测试清空所有资源池
        /// </summary>
        private void TestClearAllPools()
        {
            string gameObjectPoolKey = "TestClearGameObject";
            string genericPoolKey = "TestClearGeneric";
            
            // 创建游戏对象池
            ResourcePoolManager.Instance.CreateGameObjectPool(gameObjectPoolKey, _testPrefab, 5, 20);
            
            // 创建通用资源池
            ResourcePoolManager.Instance.CreateGenericPool<TestResource>(
                genericPoolKey, 
                () => new TestResource(),
                null,
                null,
                5, 
                20
            );
            
            // 清空所有资源池
            ResourcePoolManager.Instance.ClearAllPools();
            
            // 验证资源池是否被清空
            GameObject obj = ResourcePoolManager.Instance.SpawnGameObject(gameObjectPoolKey);
            AssertNull(obj, "Should not be able to spawn from cleared game object pool");
            
            TestResource res = ResourcePoolManager.Instance.SpawnGeneric<TestResource>(genericPoolKey);
            AssertNull(res, "Should not be able to spawn from cleared generic pool");
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
