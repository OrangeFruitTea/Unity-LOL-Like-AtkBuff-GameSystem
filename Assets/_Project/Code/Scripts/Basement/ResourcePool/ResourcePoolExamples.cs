using UnityEngine;
using System.Collections.Generic;
using Basement.ResourceManagement;

// 资源池使用示例
public class ResourcePoolExamples : MonoBehaviour
{
    [SerializeField] private GameObject _bulletPrefab;
    private const string BULLET_POOL_KEY = "BulletPool";
    private const string PATH_FINDER_POOL_KEY = "PathFinderPool";

    private void Start()
    {
        // 示例1：游戏对象池使用
        ExampleGameObjectPool();
        
        // 示例2：通用资源池使用
        ExampleGenericPool();
    }

    // 游戏对象池使用示例
    private void ExampleGameObjectPool()
    {
        // 创建子弹对象池
        ResourcePoolManager.Instance.CreateGameObjectPool(
            BULLET_POOL_KEY, 
            _bulletPrefab, 
            initialCapacity: 20,
            maxCapacity: 100
        );
        
        // 预加载子弹
        ResourcePoolManager.Instance.PreloadGameObjectPool(BULLET_POOL_KEY, 20);
        
        Debug.Log("GameObject pool created and preloaded");
    }

    // 通用资源池使用示例
    private void ExampleGenericPool()
    {
        // 创建寻路器对象池
        ResourcePoolManager.Instance.CreateGenericPool<PathFinder>(
            PATH_FINDER_POOL_KEY,
            () => new PathFinder(),
            (pathFinder) => { /* 取出时的操作 */ },
            (pathFinder) => { pathFinder.Reset(); }, // 回收时重置
            initialCapacity: 5,
            maxCapacity: 20
        );
        
        Debug.Log("Generic pool created");
    }

    // 示例：发射子弹
    public void FireBullet(Vector3 position, Quaternion rotation)
    {
        // 从池中取出子弹
        GameObject bullet = ResourcePoolManager.Instance.SpawnGameObject(
            BULLET_POOL_KEY, 
            position, 
            rotation, 
            null
        );
        
        if (bullet != null)
        {
            // 配置子弹
            Bullet bulletComponent = bullet.GetComponent<Bullet>();
            if (bulletComponent != null)
            {
                bulletComponent.Initialize(this);
            }
        }
    }

    // 示例：使用寻路器
    public void FindPath(Vector3 start, Vector3 end)
    {
        // 获取寻路器
        PathFinder pathFinder = ResourcePoolManager.Instance.SpawnGeneric<PathFinder>(PATH_FINDER_POOL_KEY);
        
        if (pathFinder != null)
        {
            // 使用寻路器
            List<Vector3> path = pathFinder.FindPath(start, end);
            
            // 执行寻路结果
            FollowPath(path);
            
            // 回收寻路器
            ResourcePoolManager.Instance.DespawnGeneric<PathFinder>(PATH_FINDER_POOL_KEY, pathFinder);
        }
    }

    private void FollowPath(List<Vector3> path)
    {
        // 实现跟随路径的逻辑
        Debug.Log($"Following path with {path.Count} points");
    }
}

// 子弹类实现IReusable接口
public class Bullet : MonoBehaviour, IReusable
{
    private ResourcePoolExamples _spawner;
    private Rigidbody _rigidbody;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    public void Initialize(ResourcePoolExamples spawner)
    {
        _spawner = spawner;
    }

    // IReusable接口实现
    public void OnSpawn()
    {
        // 重置子弹状态
        if (_rigidbody != null)
        {
            _rigidbody.velocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
        }
        gameObject.SetActive(true);
    }

    public void OnDespawn()
    {
        // 清理子弹状态
        _spawner = null;
        gameObject.SetActive(false);
    }

    private void OnCollisionEnter(Collision collision)
    {
        // 碰撞后回收
        if (_spawner != null)
        {
            ResourcePoolManager.Instance.DespawnGameObject("BulletPool", gameObject);
        }
    }
}

// 寻路器类
public class PathFinder : IReusable
{
    private List<Vector3> _pathCache = new List<Vector3>();
    
    public List<Vector3> FindPath(Vector3 start, Vector3 end)
    {
        // 简单实现，实际项目中应使用更复杂的寻路算法
        _pathCache.Clear();
        _pathCache.Add(start);
        _pathCache.Add(Vector3.Lerp(start, end, 0.5f));
        _pathCache.Add(end);
        return _pathCache;
    }
    
    public void Reset()
    {
        _pathCache.Clear();
    }
    
    // IReusable接口实现
    public void OnSpawn() 
    {
        // 取出时的操作
    }
    
    public void OnDespawn() 
    {
        Reset();
    }
}