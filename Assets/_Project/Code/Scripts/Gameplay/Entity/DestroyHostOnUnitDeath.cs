using Core.ECS;
using UnityEngine;

namespace Core.Entity
{
    /// <summary>
    /// P1：订阅 <see cref="UnitDeathEventHub"/>；仅销毁与本宿主 Bound ECS 一致的单位。ECS 释放由 <see cref="EntityBase.OnDestroy"/> 负责，避免双删。<br/>
    /// 若同物体存在 <see cref="UnitDeathRespawnBehaviour"/> 且 <see cref="UnitDeathRespawnBehaviour.SuppressHostDestroy"/> 为真，则本脚本不销毁也不隐藏宿主。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DestroyHostOnUnitDeath : MonoBehaviour
    {
        [SerializeField] private bool useDestroy = true;

        private EntityBase _host;

        private void Awake()
        {
            _host = GetComponent<EntityBase>();
        }

        private void OnEnable()
        {
            UnitDeathEventHub.UnitDied += HandleUnitDied;
        }

        private void OnDisable()
        {
            UnitDeathEventHub.UnitDied -= HandleUnitDied;
        }

        private void HandleUnitDied(EcsEntity victim, long killerEntityId)
        {
            if (_host == null || _host.BoundEcsEntity.Id == 0 || _host.BoundEcsEntity.Id != victim.Id)
                return;

            if (_host.TryGetComponent<UnitDeathRespawnBehaviour>(out var respawn) && respawn.SuppressHostDestroy)
                return;

            if (useDestroy)
                Destroy(gameObject);
            else
                gameObject.SetActive(false);
        }
    }
}
