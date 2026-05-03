using UnityEngine;

namespace Basement.Tools
{
    /// <summary>
    /// 层级挂载时保留实例在世界空间下的位置、旋转与<strong>缩放</strong>（不受父级 <see cref="Transform.lossyScale"/> 乘到子物体上）。<br/>
    /// 典型流程：先 <c>Object.Instantiate(prefab, worldPosition, worldRotation)</c>（不要直接把 parent 传入 Instantiate），再调用本类 <see cref="SetParentKeepWorldTransform"/>。
    /// </summary>
    public static class TransformPlacementUtility
    {
        /// <summary>
        /// 将 <paramref name="child"/> 挂到 <paramref name="parent"/> 下，等价于 <c>SetParent(parent, worldPositionStays: true)</c>：<br/>
        /// 父级位移/旋转/缩放变化不会改写子物体当前世界尺度；适用于父节点 scale ≠ (1,1,1) 时仍要保持预制体在世界中的大小。
        /// </summary>
        public static void SetParentKeepWorldTransform(Transform child, Transform parent)
        {
            if (child == null)
                return;

            child.SetParent(parent, true);
        }
    }
}
