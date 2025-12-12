using UnityEngine;

namespace Core.ECS
{
    public interface IEcsSystem
    {
        // 执行优先级（越小越优先）
        int UpdateOrder { get;  }
        // 系统每帧更新逻辑
        void Update();
        // 系统初始化
        void Initialize();
        //系统销毁
        void Destroy();
    }
}
