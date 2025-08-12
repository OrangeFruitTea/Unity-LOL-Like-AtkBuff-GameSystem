using UnityEngine;

namespace Core.ECS
{
    [RequireComponent(typeof(Transform))]
    public class EcsEntityBridge : MonoBehaviour
    {
        [Tooltip("关联的Ecs实体")] public EcsEntity BoundEcsEntity;
        // 快捷方法：获取组件（保留简洁性）
        public new T GetComponent<T>() where T : struct, IEcsComponent
        {
            // 明确调用EcsWorld，通过注释暴露实现
            return EcsWorld.GetComponent<T>(BoundEcsEntity);
        }

        // 快捷方法：设置组件（保留简洁性）
        public void SetComponent<T>(T component) where T : struct, IEcsComponent
        {
            EcsWorld.SetComponent(BoundEcsEntity, component);
        }


        // 补充常用操作：检查组件是否存在（避免桥接组件功能缺失）
        public bool HasComponent<T>() where T : struct, IEcsComponent
        {
            return EcsWorld.HasComponent<T>(BoundEcsEntity);
        }

        // 补充常用操作：移除组件（按需添加，避免过度封装）
        public void RemoveComponent<T>() where T : struct, IEcsComponent
        {
            EcsWorld.RemoveComponent<T>(BoundEcsEntity);
        }


        // 检查实体是否有效（包含更完整的校验）
        public bool IsValid()
        {
            return BoundEcsEntity.Id != 0 
                   && EcsWorld.Exists(BoundEcsEntity);
        }


        // 允许代码层面更新绑定的实体（如实体重建时）
        public void UpdateBoundEntity(EcsEntity newEntity)
        {
            BoundEcsEntity = newEntity;
        }
    }
}
