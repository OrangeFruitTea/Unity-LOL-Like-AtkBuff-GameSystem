namespace Basement.ResourceManagement
{
    public interface IResourcePool<T>
    {
        /// <summary>
        /// 从池中获取对象
        /// </summary>
        T Spawn();
        
        /// <summary>
        /// 尝试从池中获取对象
        /// </summary>
        bool TrySpawn(out T obj);
        
        /// <summary>
        /// 将对象回收回池中
        /// </summary>
        void Despawn(T obj);
        
        /// <summary>
        /// 预加载指定数量的对象
        /// </summary>
        void Preload(int count);
        
        /// <summary>
        /// 清空资源池
        /// </summary>
        void Clear();
        
        /// <summary>
        /// 获取当前空闲对象数量
        /// </summary>
        int IdleCount { get; }
        
        /// <summary>
        /// 获取当前已使用对象数量
        /// </summary>
        int UsedCount { get; }
    }
}