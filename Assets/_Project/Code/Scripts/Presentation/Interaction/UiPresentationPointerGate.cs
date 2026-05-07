using UnityEngine.EventSystems;

namespace Gameplay.Presentation.Interaction
{
    /// <summary>与设计文档 §9 一致：指针在 EventSystem UI 上时短路世界射线/悬停。</summary>
    public static class UiPresentationPointerGate
    {
        public static bool IsPointerOverUi()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }
    }
}
