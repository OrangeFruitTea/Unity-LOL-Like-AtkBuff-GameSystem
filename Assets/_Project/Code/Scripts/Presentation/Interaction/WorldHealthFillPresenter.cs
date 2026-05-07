using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.Presentation.Interaction
{
    /// <summary>Image(Filled) 血量填充（设计文档 §4.1）；数值由战斗层或桥接脚本调用 <see cref="SetHp"/>。</summary>
    [DisallowMultipleComponent]
    public sealed class WorldHealthFillPresenter : MonoBehaviour
    {
        [SerializeField]
        private Image fillImage;

        public void SetFill01(float ratio01)
        {
            if (fillImage == null)
                return;
            fillImage.fillAmount = Mathf.Clamp01(ratio01);
        }

        public void SetHp(float current, float max)
        {
            if (max <= 1e-5f)
            {
                SetFill01(0f);
                return;
            }

            SetFill01(current / max);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (fillImage == null)
                fillImage = GetComponentInChildren<Image>(true);
        }
#endif
    }
}
