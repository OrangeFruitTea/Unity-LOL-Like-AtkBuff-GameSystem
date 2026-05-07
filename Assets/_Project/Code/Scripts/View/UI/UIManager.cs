using System;
using System.Collections.Generic;
using Core.ECS;
using UnityEngine;
using UnityEngine.UI;
using Basement.Utils;
using Widgets.PlayerStatement;

namespace Core.UI
{
    /// <summary>
    /// 层级容器：一层一个 <see cref="Canvas"/> 根节点；不再为每种 <see cref="UIAnchorType"/> 创建子物体。
    /// </summary>
    public class UILayerContainer
    {
        public GameObject LayerObject { get; set; }

        public Canvas LayerCanvas { get; set; }

        public UILayerContainer(GameObject layerObj, Canvas canvas)
        {
            LayerObject = layerObj;
            LayerCanvas = canvas;
        }
    }

    /// <summary>
    /// UI管理器（同时支持层级控制和锚点控制）
    /// </summary>
    public class UIManager : Singleton<UIManager>
    {
        private GameObject _uiRoot;  // 全局UI根节点
        private Dictionary<UILayerType, UILayerContainer> _layerContainers = new Dictionary<UILayerType, UILayerContainer>();
        private Dictionary<Guid, GameObject> _activeUiInstances = new Dictionary<Guid, GameObject>();
        private Dictionary<Guid, UILayerType> _uiInstanceLayers = new Dictionary<Guid, UILayerType>();
        private Dictionary<Guid, UIAnchorType> _uiInstanceAnchors = new Dictionary<Guid, UIAnchorType>();

        protected override void Awake()
        {
            base.Awake();
            InitializeUIRoot();
        }

        /// <summary>
        /// 初始化全局UI根节点
        /// </summary>
        private void InitializeUIRoot()
        {
            // 查找或创建UI根节点
            _uiRoot = GameObject.Find("[UIRoot]");
            if (_uiRoot == null)
            {
                _uiRoot = new GameObject("[UIRoot]");
                DontDestroyOnLoad(_uiRoot);
            }

            // 确保根节点不参与物理碰撞和渲染
            _uiRoot.layer = LayerMask.NameToLayer("UI");
            var rootRect = _uiRoot.GetComponent<RectTransform>();
            if (rootRect == null) rootRect = _uiRoot.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
        }

        /// <summary>
        /// 创建指定类型的层级（Canvas 根）。界面实例直接挂在该根下，锚点仅作用于实例 <see cref="RectTransform"/>。
        /// </summary>
        private UILayerContainer CreateLayer(UILayerType layerType)
        {
            if (_layerContainers.TryGetValue(layerType, out var layer))
            {
                Debug.LogWarning($"UI层级 {layerType} 已存在，无需重复创建");
                return layer;
            }

            // 1. 创建层级根对象
            var layerObj = new GameObject($"Layer_{layerType}");
            layerObj.transform.SetParent(_uiRoot.transform);
            layerObj.layer = LayerMask.NameToLayer("UI");

            // 2. 添加Canvas组件（控制渲染顺序）
            var canvas = layerObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = (int)layerType; // 用枚举值作为排序值，确保层级优先级
            canvas.overrideSorting = true;
            // 枚举值亦是全局 Overlay 排序（如 HUD=30）。预制体内子 Canvas 若 Override Sorting 且 order 更小，
            // 会先被本层整块后绘盖住；单挂在场景另一条 Canvas 上时常见问题不明显。

            // 3. 添加CanvasScaler（适配不同分辨率）
            var scaler = layerObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); // 设计分辨率

            // 4. 添加GraphicRaycaster（支持UI交互）
            layerObj.AddComponent<GraphicRaycaster>();

            var layerRect = layerObj.GetComponent<RectTransform>();
            layerRect.anchorMin = Vector2.zero;
            layerRect.anchorMax = Vector2.one;
            layerRect.offsetMin = Vector2.zero;
            layerRect.offsetMax = Vector2.zero;

            var layerContainer = new UILayerContainer(layerObj, canvas);
            _layerContainers[layerType] = layerContainer;
            return layerContainer;
        }

        /// <summary>
        /// 按 <see cref="UIAnchorType"/> 设置实例 <see cref="RectTransform"/>（相对层级 Canvas 根的全屏区域）。
        /// </summary>
        private void SetAnchorRectTransform(RectTransform rect, UIAnchorType anchorType)
        {
            rect.sizeDelta = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);

            switch (anchorType)
            {
                case UIAnchorType.Center:
                    rect.anchorMin = new Vector2(0.5f, 0.5f);
                    rect.anchorMax = new Vector2(0.5f, 0.5f);
                    rect.anchoredPosition = Vector2.zero;
                    break;
                case UIAnchorType.TopLeft:
                    rect.anchorMin = Vector2.zero;
                    rect.anchorMax = Vector2.zero;
                    rect.anchoredPosition = new Vector2(0, Screen.height);
                    break;
                case UIAnchorType.TopCenter:
                    rect.anchorMin = new Vector2(0.5f, 1f);
                    rect.anchorMax = new Vector2(0.5f, 1f);
                    rect.anchoredPosition = Vector2.zero;
                    break;
                case UIAnchorType.TopRight:
                    rect.anchorMin = Vector2.one;
                    rect.anchorMax = Vector2.one;
                    rect.anchoredPosition = new Vector2(-Screen.width, 0);
                    break;
                case UIAnchorType.MiddleLeft:
                    rect.anchorMin = new Vector2(0f, 0.5f);
                    rect.anchorMax = new Vector2(0f, 0.5f);
                    rect.anchoredPosition = Vector2.zero;
                    break;
                case UIAnchorType.MiddleRight:
                    rect.anchorMin = new Vector2(1f, 0.5f);
                    rect.anchorMax = new Vector2(1f, 0.5f);
                    rect.anchoredPosition = new Vector2(-Screen.width, 0);
                    break;
                case UIAnchorType.BottomLeft:
                    rect.anchorMin = Vector2.zero;
                    rect.anchorMax = Vector2.zero;
                    rect.anchoredPosition = Vector2.zero;
                    break;
                case UIAnchorType.BottomCenter:
                    rect.anchorMin = new Vector2(0.5f, 0f);
                    rect.anchorMax = new Vector2(0.5f, 0f);
                    rect.anchoredPosition = Vector2.zero;
                    break;
                case UIAnchorType.BottomRight:
                    rect.anchorMin = new Vector2(1f, 0f);
                    rect.anchorMax = new Vector2(1f, 0f);
                    rect.anchoredPosition = new Vector2(-Screen.width, 0);
                    break;
            }
        }

        /// <summary>
        /// 生成UI实例（核心方法：按层级和锚点定位）
        /// </summary>
        public bool GenerateUI(UIElement element)
        {
            if (element.IsGenerated || string.IsNullOrEmpty(element.PrefabPath))
            {
                Debug.LogWarning("UI元素已生成或预制体路径为空");
                return false;
            }

            try
            {
                // 1. 加载预制体
                var prefab = Resources.Load<GameObject>(element.PrefabPath);
                if (prefab == null)
                {
                    Debug.LogError($"预制体加载失败：{element.PrefabPath}");
                    return false;
                }

                // 2. 获取或创建目标层级
                if (!_layerContainers.TryGetValue(element.LayerType, out var targetLayer))
                {
                    targetLayer = CreateLayer(element.LayerType);
                }

                // 3. 挂在层级 Canvas 根下（每层单一容器，无 Anchor_* 子节点）
                var uiInstance = Instantiate(prefab, targetLayer.LayerObject.transform);
                uiInstance.name = $"{prefab.name}_{Guid.NewGuid().ToString().Substring(0, 8)}";

                // 4. 适配锚点与尺寸
                var rectTransform = uiInstance.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    // 应用锚点设置（确保与容器对齐方式一致）
                    SetAnchorRectTransform(rectTransform, element.AnchorType);
                    // 应用位置偏移（相对于锚点）
                    rectTransform.anchoredPosition = element.Position;
                    // 应用尺寸（为Zero时使用预制体默认尺寸）
                    if (element.Size != Vector2.zero)
                    {
                        rectTransform.sizeDelta = element.Size;
                    }
                }

                // 5. 处理跨场景保留
                if (element.DontDestroyOnLoad)
                {
                    DontDestroyOnLoad(uiInstance);
                }

                // 6. 更新元素状态
                element.IsGenerated = true;
                element.UiInstanceId = Guid.NewGuid();
                _activeUiInstances[element.UiInstanceId] = uiInstance;
                _uiInstanceLayers[element.UiInstanceId] = element.LayerType;
                _uiInstanceAnchors[element.UiInstanceId] = element.AnchorType;

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"UI生成失败：{ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// <see cref="Resources.Load{T}(string)"/> 使用的路径（无扩展名）。对应 <c>Assets/_Project/Resources/UI/Widgets/Game/Statement/DetailStatement.prefab</c>。
        /// </summary>
        public const string DetailStatementResourcePath = "UI/Widgets/Game/Statement/DetailStatement";

        /// <summary>
        /// 生成局内属性详情预制体（DetailStatement）。需在工程中存在上述 Resources 资源。
        /// </summary>
        public bool TrySpawnDetailStatement(
            out Guid instanceId,
            out GameObject root,
            UILayerType layerType = UILayerType.HUD,
            UIAnchorType anchorType = UIAnchorType.BottomLeft,
            Vector2 position = default,
            Vector2 size = default,
            bool dontDestroyOnLoad = false)
        {
            root = null;
            var element = new UIElement();
            element.SetDefaults();
            element.PrefabPath = DetailStatementResourcePath;
            element.LayerType = layerType;
            element.AnchorType = anchorType;
            element.Position = position;
            element.Size = size;
            element.DontDestroyOnLoad = dontDestroyOnLoad;

            if (!GenerateUI(element))
            {
                instanceId = Guid.Empty;
                Debug.LogError($"[UIManager] DetailStatement 生成失败（Resources 路径: {DetailStatementResourcePath}）。");
                return false;
            }

            instanceId = element.UiInstanceId;
            if (!_activeUiInstances.TryGetValue(instanceId, out root) || root == null)
            {
                instanceId = Guid.Empty;
                Debug.LogError("[UIManager] GenerateUI 返回成功但未登记实例根物体。");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Resources 路径（无扩展名）。对应 <c>Assets/_Project/Resources/UI/Widgets/Game/Statement/StatementWidget.prefab</c>。
        /// </summary>
        public const string StatementWidgetResourcePath = StatementWidgetElement.StatementWidgetResourcePath;

        /// <summary>
        /// 生成完整 Statement 预制（血/蓝条、Buff 根、属性详情）；见 <see cref="StatementWidget"/>。
        /// </summary>
        public bool TrySpawnStatementWidget(
            out Guid instanceId,
            out GameObject root,
            UILayerType layerType = UILayerType.HUD,
            UIAnchorType anchorType = UIAnchorType.Center,
            Vector2 position = default,
            Vector2 size = default,
            bool dontDestroyOnLoad = false)
        {
            root = null;
            var element = StatementWidgetElement.CreateForHud(layerType, anchorType, position, size, dontDestroyOnLoad);

            if (!GenerateUI(element))
            {
                instanceId = Guid.Empty;
                Debug.LogError($"[UIManager] StatementWidget 生成失败（Resources 路径: {StatementWidgetResourcePath}）。");
                return false;
            }

            instanceId = element.UiInstanceId;
            if (!_activeUiInstances.TryGetValue(instanceId, out root) || root == null)
            {
                instanceId = Guid.Empty;
                Debug.LogError("[UIManager] GenerateUI 返回成功但未登记 StatementWidget 根物体。");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 对子层级中所有 <see cref="IEntityBridgeBindable"/> 调用 <see cref="IEntityBridgeBindable.Bind"/>。
        /// （Unity 的 <see cref="Component.GetComponentsInChildren{T}(bool)"/> 无法按接口类型查找，故遍历 <see cref="MonoBehaviour"/>。）
        /// </summary>
        public void BindEcsBridgeConsumers(GameObject uiRoot, EcsEntityBridge bridge)
        {
            if (uiRoot == null || bridge == null || !bridge.IsValid())
                return;

            var behaviours = uiRoot.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (var mb in behaviours)
            {
                if (mb is IEntityBridgeBindable bindable)
                    bindable.Bind(bridge);
            }
        }

        /// <summary>
        /// 销毁指定UI实例
        /// </summary>
        public void DestroyUI(Guid uiInstanceId)
        {
            if (_activeUiInstances.TryGetValue(uiInstanceId, out var instance))
            {
                Destroy(instance);
                _activeUiInstances.Remove(uiInstanceId);
                _uiInstanceLayers.Remove(uiInstanceId);
                _uiInstanceAnchors.Remove(uiInstanceId);
            }
        }

        /// <summary>
        /// 销毁指定层级的所有UI
        /// </summary>
        public void ClearLayer(UILayerType layerType)
        {
            var instancesToRemove = new List<Guid>();
            foreach (var (id, layer) in _uiInstanceLayers)
            {
                if (layer == layerType)
                {
                    instancesToRemove.Add(id);
                }
            }

            foreach (var id in instancesToRemove)
            {
                DestroyUI(id);
            }
        }

        /// <summary>
        /// 销毁指定层级+锚点的所有UI
        /// </summary>
        public void ClearLayerAnchor(UILayerType layerType, UIAnchorType anchorType)
        {
            var instancesToRemove = new List<Guid>();
            foreach (var (id, layer) in _uiInstanceLayers)
            {
                if (layer != layerType)
                    continue;
                if (_uiInstanceAnchors.TryGetValue(id, out var a) && a == anchorType)
                    instancesToRemove.Add(id);
            }

            foreach (var id in instancesToRemove)
                DestroyUI(id);
        }

        /// <summary>
        /// 获取指定层级的所有UI实例
        /// </summary>
        public List<GameObject> GetLayerUIInstances(UILayerType layerType)
        {
            var instances = new List<GameObject>();
            foreach (var (id, instance) in _activeUiInstances)
            {
                if (_uiInstanceLayers.TryGetValue(id, out var layer) && layer == layerType)
                {
                    instances.Add(instance);
                }
            }
            return instances;
        }

        /// <summary>
        /// 全局清理（游戏退出时调用）
        /// </summary>
        public void Cleanup()
        {
            foreach (var instance in _activeUiInstances.Values)
            {
                Destroy(instance);
            }
            _activeUiInstances.Clear();
            _uiInstanceLayers.Clear();
            _uiInstanceAnchors.Clear();

            if (_uiRoot != null)
            {
                Destroy(_uiRoot);
            }
            _layerContainers.Clear();
        }
    }
}
