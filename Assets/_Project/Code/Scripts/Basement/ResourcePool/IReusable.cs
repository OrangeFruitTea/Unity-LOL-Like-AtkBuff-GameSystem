namespace Basement.ResourceManagement
{
    public interface IReusable
    {
        /// <summary>
        /// 当对象被从池中取出时调用
        /// </summary>
        void OnSpawn();
        
        /// <summary>
        /// 当对象被回收回池中时调用
        /// </summary>
        void OnDespawn();
    }
}