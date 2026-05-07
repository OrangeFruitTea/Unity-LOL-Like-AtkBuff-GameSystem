using UnityEngine;

namespace Gameplay.Presentation.Interaction
{
    /// <summary>
    /// 可选：仅 Hover / Selected 时显示 World Canvas（设计文档 §4.3）。<br/>
    /// 将 <see cref="unitRoot"/> 设为与射线解析出的悬停根相同的 Transform（通常为带 <see cref="RimOutlineDriver"/> 的单位根）。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WorldHealthBarPresentationFocusGate : MonoBehaviour
    {
        [SerializeField]
        private Canvas targetCanvas;

        [SerializeField]
        private PresentationSelectionHub hub;

        [SerializeField]
        private Transform unitRoot;

        private bool _hubSubscribed;

        private void Awake()
        {
            if (targetCanvas == null)
                targetCanvas = GetComponent<Canvas>();
            if (unitRoot == null)
                unitRoot = transform.root;
            ApplyVisible(false);
        }

        private void OnEnable()
        {
            TryBindHub();
            Refresh();
        }

        private void OnDisable()
        {
            TearDownHub();
        }

        private void LateUpdate()
        {
            TryBindHub();
        }

        private void OnDestroy()
        {
            TearDownHub();
        }

        private void TearDownHub()
        {
            if (hub != null && _hubSubscribed)
            {
                hub.HoverChanged -= OnHoverChanged;
                hub.SelectionChanged -= OnSelectionChanged;
            }

            _hubSubscribed = false;
        }

        private void TryBindHub()
        {
            if (_hubSubscribed)
                return;

            if (hub == null)
                hub = PresentationSelectionHub.Instance;
            if (hub == null)
                return;

            hub.HoverChanged += OnHoverChanged;
            hub.SelectionChanged += OnSelectionChanged;
            _hubSubscribed = true;
            Refresh();
        }

        private void OnHoverChanged(Transform prev, Transform next)
        {
            Refresh();
        }

        private void OnSelectionChanged(Transform prev, Transform next)
        {
            Refresh();
        }

        private void Refresh()
        {
            if (targetCanvas == null || hub == null || unitRoot == null)
                return;

            var show = ReferenceEquals(hub.HoverRoot, unitRoot) || ReferenceEquals(hub.SelectedRoot, unitRoot);
            ApplyVisible(show);
        }

        private void ApplyVisible(bool visible)
        {
            if (targetCanvas != null)
                targetCanvas.enabled = visible;
        }
    }
}
