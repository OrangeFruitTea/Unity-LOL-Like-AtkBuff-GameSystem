using Core.ECS;
using Core.UI;
using UnityEngine;

namespace Widgets.PlayerStatement
{
    /// <summary>Buff 列表根；运行时注入 <see cref="EcsEntityBridge"/>；动态生成时清空预制体内的占位条目。</summary>
    public sealed class BuffItemManager : MonoBehaviour, IEntityBridgeBindable
    {
        public EcsEntityBridge ecsBridge;

        /// <summary>若条目挂在子节点而非本物体下，赋值该容器（默认同 <see cref="Transform"/>）</summary>
        [SerializeField] private RectTransform contentRoot;

        private void Awake() => ClearEntries();

        public void SetEntityBridge(EcsEntityBridge bridge) => Bind(bridge);

        /// <inheritdoc />
        public void Bind(EcsEntityBridge bridge)
        {
            ClearEntries();
            ecsBridge = bridge;
        }

        /// <summary>移除列表下所有子物体（如预制里为排版预览保留的 BuffItem）。</summary>
        public void ClearEntries()
        {
            var root = contentRoot != null ? contentRoot : transform as RectTransform;
            if (root == null)
                return;

            for (var i = root.childCount - 1; i >= 0; i--)
            {
                var child = root.GetChild(i);
                if (child != null)
                    Destroy(child.gameObject);
            }
        }
    }
}
