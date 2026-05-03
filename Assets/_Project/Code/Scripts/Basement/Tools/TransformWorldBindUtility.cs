using UnityEngine;

namespace Basement.Tools
{
    /// <summary>
    /// 将物体挂到任意 <see cref="Transform"/> 下时，拆解父链缩放/层级对「世界空间位姿与缩放」的非预期继承。
    /// 适用于父节点 <c>localScale ≠ (1,1,1)</c> 时仍需子物体世界缩放与 Prefab 一致等场景。
    /// </summary>
    public static class TransformWorldBindUtility
    {
        /// <summary>
        /// 先在场景根层级按世界坐标实例化，再 <c>SetParent(parent, worldPositionStays: true)</c>。
        /// 与 <c>Object.Instantiate(prefab, position, rotation, parent)</c> 不同：后者会把父级缩放乘进子物体世界缩放。
        /// </summary>
        /// <param name="prefab">预制体或模板。</param>
        /// <param name="worldPosition">世界位置。</param>
        /// <param name="worldRotation">世界旋转。</param>
        /// <param name="parentOrNull">为 <c>null</c> 时不改层级（等价于普通 Instantiate）。</param>
        public static GameObject InstantiateWithWorldPoseThenParent(
            GameObject prefab,
            Vector3 worldPosition,
            Quaternion worldRotation,
            Transform parentOrNull)
        {
            var instance = Object.Instantiate(prefab, worldPosition, worldRotation);
            if (parentOrNull != null)
                instance.transform.SetParent(parentOrNull, worldPositionStays: true);
            return instance;
        }

        /// <summary>
        /// 将现有物体改挂到 <paramref name="newParent"/>，保持当前世界位置、旋转与缩放不变。
        /// </summary>
        public static void ReparentPreservingWorldTransform(Transform child, Transform newParent)
        {
            if (child == null)
                return;
            child.SetParent(newParent, worldPositionStays: true);
        }
    }
}
