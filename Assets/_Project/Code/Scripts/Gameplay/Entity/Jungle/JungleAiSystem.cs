using Core.ECS;

namespace Core.Entity.Jungle
{
    /// <summary> 野怪租赁、追逐、普攻写黑板；见设计文档 §7、§10。毕设可先留空。</summary>
    public sealed class JungleAiSystem : IEcsSystem
    {
        public int UpdateOrder => 38;

        public void Initialize()
        {
        }

        public void Destroy()
        {
        }

        public void Update()
        {
        }
    }
}
