using Core.ECS;
using Core.UI;
using UnityEngine;

namespace Widgets.PlayerStatement
{
    /// <summary>
    /// 局内 Statement 预制根：<see cref="SliderBarController"/>（血/蓝）、<see cref="BuffItemManager"/>、<see cref="DetailStatItemManager"/> 的统一 ECS 绑定入口。
    /// </summary>
    public sealed class StatementWidget : MonoBehaviour, IEntityBridgeBindable
    {
        [Header("Bars")]
        [SerializeField] private SliderBarController healthBarSlider;
        [SerializeField] private SliderBarController manaBarSlider;

        [Header("Lists / Detail")]
        [SerializeField] private BuffItemManager buffList;
        [SerializeField] private DetailStatItemManager detailStatementManager;

        private void Awake()
        {
            ResolveOptionalRefs();
        }

        /// <summary>补足未在 Inspector 拖拽的 <see cref="BuffItemManager"/>。血/蓝条与属性详情须在预制上显式指定。</summary>
        private void ResolveOptionalRefs()
        {
            if (buffList == null)
                buffList = GetComponentInChildren<BuffItemManager>(true);
        }

        /// <inheritdoc />
        public void Bind(EcsEntityBridge bridge)
        {
            if (bridge == null || !bridge.IsValid())
                return;

            ResolveOptionalRefs();

            healthBarSlider?.SetEntityBridge(bridge);
            manaBarSlider?.SetEntityBridge(bridge);
            buffList?.Bind(bridge);
            detailStatementManager?.Bind(bridge);
        }
    }

    /// <summary>
    /// <see cref="UIManager.GenerateUI(Core.UI.UIElement)"/> 用的 Statement 树根描述；路径对应 <c>Assets/_Project/Resources/UI/Widgets/Game/Statement/StatementWidget.prefab</c>。
    /// </summary>
    public sealed class StatementWidgetElement : UIElement
    {
        public const string StatementWidgetResourcePath = "UI/Widgets/Game/Statement/StatementWidget";

        /// <summary>HUD 默认居中锚点。<see cref="UIManager.GenerateUI"/> 会按 <see cref="UIAnchorType"/> 设置根的 <c>sizeDelta</c>；若未传入非零 <see cref="UIElement.Size"/>，在点锚下根区域面积为 0。此处默认与 <see cref="UIManager.CreateLayer"/> 中 <see cref="CanvasScaler.referenceResolution"/>（1920×1080）一致，避免整块 HUD 不可见。</summary>
        public static StatementWidgetElement CreateForHud(
            UILayerType layerType = UILayerType.HUD,
            UIAnchorType anchorType = UIAnchorType.Center,
            Vector2 position = default,
            Vector2 size = default,
            bool dontDestroyOnLoad = false)
        {
            var el = new StatementWidgetElement();
            el.SetDefaults();
            el.PrefabPath = StatementWidgetResourcePath;
            el.LayerType = layerType;
            el.AnchorType = anchorType;
            el.Position = position;
            el.Size = size == Vector2.zero ? new Vector2(1920, 1080) : size;
            el.DontDestroyOnLoad = dontDestroyOnLoad;
            return el;
        }
    }
}
