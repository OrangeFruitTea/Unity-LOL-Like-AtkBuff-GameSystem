using System;
using UnityEngine;

namespace Gameplay.Presentation.Interaction
{
    [DisallowMultipleComponent]
    public sealed class PresentationSelectionHub : MonoBehaviour
    {
        public static PresentationSelectionHub Instance { get; private set; }

        [SerializeField]
        private Camera targetCamera;

        [SerializeField]
        private bool pollHoverAutomatically = true;

        [SerializeField]
        private LayerMask selectableLayers = ~0;

        [SerializeField]
        private float hoverRayDistance = 500f;

        [SerializeField]
        private bool suppressHoverWhenPointerOverUi = true;

        private Transform _hoverRoot;
        private Transform _selectedRoot;

        public Transform HoverRoot => _hoverRoot;

        public Transform SelectedRoot => _selectedRoot;

        public event Action<Transform, Transform> HoverChanged;

        public event Action<Transform, Transform> SelectionChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning($"[PresentationSelectionHub] Duplicate instance — destroying '{name}'.");
                Destroy(gameObject);
                return;
            }

            Instance = this;
            if (targetCamera == null)
                targetCamera = Camera.main;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void Start()
        {
            RimOutlineDriver.SyncAll(_hoverRoot, _selectedRoot);
        }

        private void LateUpdate()
        {
            if (!pollHoverAutomatically)
                return;
            RefreshHoverScreen(Input.mousePosition);
        }

        public void RefreshHoverScreen(Vector3 screenPosition)
        {
            if (suppressHoverWhenPointerOverUi && UiPresentationPointerGate.IsPointerOverUi())
            {
                SetHover(null);
                return;
            }

            if (!TryRaySelectableRoot(screenPosition, out var root))
                SetHover(null);
            else
                SetHover(root);
        }

        /// <returns>射线是否命中并可解析为可选中根节点。</returns>
        public bool TryRaySelectableRoot(Vector3 screenPosition, out Transform selectableRoot)
        {
            selectableRoot = null;
            EnsureCamera();

            if (targetCamera == null)
                return false;

            var ray = targetCamera.ScreenPointToRay(screenPosition);
            var layerMask = selectableLayers.value != 0 ? selectableLayers.value : Physics.DefaultRaycastLayers;
            if (!Physics.Raycast(ray, out var hit, hoverRayDistance, layerMask, QueryTriggerInteraction.Ignore))
                return false;

            selectableRoot = SelectablePresentationResolve.TryResolveRoot(hit.collider);
            return selectableRoot != null;
        }

        /// <summary>与设计文档一致的左键点选挂载点（由输入层在非 UI 指针时调用）。</summary>
        public bool TryCommitSelectionUnderScreenPoint(Vector3 screenPosition)
        {
            if (!TryRaySelectableRoot(screenPosition, out var root))
            {
                ClearSelection();
                return false;
            }

            SetSelected(root);
            return true;
        }

        public void SetHover(Transform nextHoverRoot)
        {
            var prev = _hoverRoot;
            if (prev == nextHoverRoot)
                return;

            _hoverRoot = nextHoverRoot;
            HoverChanged?.Invoke(prev, _hoverRoot);
            RimOutlineDriver.SyncAll(_hoverRoot, _selectedRoot);
        }

        public void SetSelected(Transform nextSelectedRoot)
        {
            var prev = _selectedRoot;
            if (prev == nextSelectedRoot)
                return;

            _selectedRoot = nextSelectedRoot;
            SelectionChanged?.Invoke(prev, _selectedRoot);
            RimOutlineDriver.SyncAll(_hoverRoot, _selectedRoot);
        }

        public void ClearSelection()
        {
            SetSelected(null);
        }

        /// <summary>单位 <see cref="RimOutlineDriver"/> 启用后同步当前 Hover/Selected 的视觉状态。</summary>
        public void RefreshRimVisuals()
        {
            RimOutlineDriver.SyncAll(_hoverRoot, _selectedRoot);
        }

        private void EnsureCamera()
        {
            if (targetCamera == null)
                targetCamera = Camera.main;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            EnsureCamera();
        }
#endif
    }
}
