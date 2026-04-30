using Core.Entity;
using Core.ECS;
using UnityEngine;

namespace Core.Entity.Jungle
{
    /// <summary>
    /// 野怪 Prefab：注册后挂载 <see cref="JungleCreepModuleComponent"/>；
    /// 可选将出生后位置写入 <see cref="JungleCreepModuleComponent.LeashCenter"/>。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class JungleCreepEcsAttachments : MonoBehaviour, IEntitySpawnExtension
    {
        [SerializeField] private int campId;
        [SerializeField] private byte anchorSlotIndex;
        [SerializeField] private float leashRadius = 8f;

        [Tooltip("若为真，则用宿主 Transform.position 填充 LeashCenter（否则需在表里预填或通过其它手段写入）")]
        [SerializeField] private bool leashCenterFromSpawnPosition = true;

        public void OnAfterEcsBaseSpawned(EcsEntity ecs, EntityBase host)
        {
            var jungle = new JungleCreepModuleComponent();
            jungle.InitializeDefaults();
            jungle.CampId = campId;
            jungle.AnchorSlotIndex = anchorSlotIndex;
            jungle.LeashRadius = leashRadius;
            jungle.CurrentState = JungleCreepState.Idle;

            if (leashCenterFromSpawnPosition && host != null)
                jungle.LeashCenter = host.transform.position;

            EcsWorld.AddComponent(ecs, jungle);
        }
    }
}
