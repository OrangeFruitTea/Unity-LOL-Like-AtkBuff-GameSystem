using Core.ECS;
using UnityEngine;

namespace Core.Entity.Jungle
{
    /// <summary>
    /// 野区单位：营地、租赁、占位槽。数值仍走 <see cref="Core.Entity.EntityDataComponent"/>。
    /// 设计文档 §7；《MOBA局内经济系统设计文档》可挂 Profile 于同实体侧表。
    /// </summary>
    public struct JungleCreepModuleComponent : IEcsComponent
    {
        /// <summary> 营地 id；同营地共用脱战逻辑。 </summary>
        public int CampId;

        /// <summary> 租赁圆心世界坐标（可在生成时写入）。也可用表 id + 运行时解析替换。 </summary>
        public Vector3 LeashCenter;

        /// <summary> 超出半径则脱战回巢。 </summary>
        public float LeashRadius;

        public JungleCreepState CurrentState;

        /// <summary> 大营地内第几只。 </summary>
        public byte AnchorSlotIndex;

        public void InitializeDefaults()
        {
            CampId = 0;
            LeashCenter = Vector3.zero;
            LeashRadius = 8f;
            CurrentState = JungleCreepState.Idle;
            AnchorSlotIndex = 0;
        }
    }
}
