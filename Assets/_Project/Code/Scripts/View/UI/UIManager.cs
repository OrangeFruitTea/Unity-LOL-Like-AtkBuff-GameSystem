using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Basement.Utils;

namespace Core.UI
{
    /// <summary>
    /// 层级容器数据（每个层级包含独立Canvas和锚点容器）
    /// </summary>
    public class UILayerContainer
    {
        public GameObject LayerObject { get; set; }  // 层级根对象
        public Canvas LayerCanvas { get; set; }      // 层级Canvas（控制渲染顺序）
        public Dictionary<UIAnchorType, RectTransform> AnchorContainers { get; set; } // 该层级下的所有锚点

        public UILayerContainer(GameObject layerObj, Canvas canvas)
        {
            LayerObject = layerObj;
            LayerCanvas = canvas;
            AnchorContainers = new Dictionary<UIAnchorType, RectTransform>();
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
        private Dictionary<Guid, UILayerType> _uiInstanceLayers = new Dictionary<Guid, UILayerType>(); // 记录UI实例所属层级

        protected override void Awake()
        {
            base.Awake();
            InitializeUIRoot();
            InitializeDefaultLayers();
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
        /// 初始化默认层级（按预设的UILayerType创建）
        /// </summary>
        private void InitializeDefaultLayers()
        {
            foreach (UILayerType layerType in Enum.GetValues(typeof(UILayerType)))
            {
                CreateLayer(layerType);
            }
        }

        /// <summary>
        /// 创建指定类型的层级（包含Canvas和锚点容器）
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

            // 3. 添加CanvasScaler（适配不同分辨率）
            var scaler = layerObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); // 设计分辨率

            // 4. 添加GraphicRaycaster（支持UI交互）
            layerObj.AddComponent<GraphicRaycaster>();

            // 5. 为当前层级创建所有锚点容器
            var layerContainer = new UILayerContainer(layerObj, canvas);
            foreach (UIAnchorType anchorType in Enum.GetValues(typeof(UIAnchorType)))
            {
                var anchorObj = new GameObject($"Anchor_{anchorType}");
                anchorObj.transform.SetParent(layerObj.transform);
                var anchorRect = anchorObj.AddComponent<RectTransform>();
                SetAnchorRectTransform(anchorRect, anchorType);
                layerContainer.AnchorContainers[anchorType] = anchorRect;
            }

            _layerContainers[layerType] = layerContainer;
            return layerContainer;
        }

        /// <summary>
        /// 设置锚点容器的RectTransform（核心适配逻辑）
        /// </summary>
        private void SetAnchorRectTransform(RectTransform rect, UIAnchorType anchorType)
        {
            rect.sizeDelta = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f); // 锚点容器自身中心点为原点

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

                // 3. 获取目标锚点容器
                if (!targetLayer.AnchorContainers.TryGetValue(element.AnchorType, out var targetAnchor))
                {
                    Debug.LogError($"锚点 {element.AnchorType} 在层级 {element.LayerType} 中不存在");
                    return false;
                }

                // 4. 实例化UI并设置父节点
                var uiInstance = Instantiate(prefab, targetAnchor);
                uiInstance.name = $"{prefab.name}_{Guid.NewGuid().ToString().Substring(0, 8)}"; // 避免重名

                // 5. 适配位置和尺寸
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

                // 6. 处理跨场景保留
                if (element.DontDestroyOnLoad)
                {
                    DontDestroyOnLoad(uiInstance);
                }

                // 7. 更新元素状态
                element.IsGenerated = true;
                element.UiInstanceId = Guid.NewGuid();
                _activeUiInstances[element.UiInstanceId] = uiInstance;
                _uiInstanceLayers[element.UiInstanceId] = element.LayerType;

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"UI生成失败：{ex.Message}");
                return false;
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
            if (!_layerContainers.TryGetValue(layerType, out var layer)) return;
            if (!layer.AnchorContainers.TryGetValue(anchorType, out var anchor)) return;

            var instancesToRemove = new List<Guid>();
            foreach (var (id, instance) in _activeUiInstances)
            {
                if (instance.transform.parent == anchor)
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

            if (_uiRoot != null)
            {
                Destroy(_uiRoot);
            }
            _layerContainers.Clear();
        }
    }
}
