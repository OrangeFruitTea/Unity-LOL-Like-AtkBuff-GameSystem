using UnityEngine;

namespace Gameplay.Presentation.Interaction
{
    /// <summary>
    /// 轻量：左键调用 <see cref="PresentationSelectionHub.TryCommitSelectionUnderScreenPoint"/>，触发 <see cref="PresentationSelectionHub.SelectionChanged"/>。<br/>
    /// 悬停由 Hub 的 <see cref="PresentationSelectionHub.RefreshHoverScreen"/>（默认每帧自动）负责，无需再写射线。
    /// </summary>
    public sealed class PresentationSelectionClickForwarder : MonoBehaviour
    {
        [SerializeField]
        private PresentationSelectionHub hub;

        [SerializeField]
        private int mouseButton;

        private void Update()
        {
            if (!Input.GetMouseButtonDown(mouseButton))
                return;
            if (UiPresentationPointerGate.IsPointerOverUi())
                return;

            if (hub == null)
                hub = PresentationSelectionHub.Instance;
            if (hub == null)
                return;

            hub.TryCommitSelectionUnderScreenPoint(Input.mousePosition);
        }
    }
}
