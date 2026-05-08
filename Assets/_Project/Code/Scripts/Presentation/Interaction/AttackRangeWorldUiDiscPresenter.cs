using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.Presentation.Interaction
{
    /// <summary>
    /// 世界空间 Canvas 下的 <see cref="Image"/>：贴地平铺。直径缩放按 Inspector 的基准攻击半径在 <b>Awake</b> 与 <b>OnValidate</b> 各算一次（不按帧改缩放）。<br/>
    /// <see cref="SetWorldDisc"/> 只更新位置与透明度。<br/>
    /// 层级：<c>presentationRoot</c> → Canvas（World Space）→ Image。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AttackRangeWorldUiDiscPresenter : MonoBehaviour
    {
        [Tooltip("被缩放、移动、旋转的根物体（一般为 Canvas 的父物体或 Canvas 本体）。")]
        [SerializeField]
        private Transform presentationRoot;

        [Tooltip("要显示的 Sprite，一般为 Canvas 子物体上的 Image。")]
        [SerializeField]
        private Image discImage;

        [Header("Diameter scale (Awake / OnValidate only)")]
        [Tooltip("与 Image 正方形 Rect 边长一致（与 referenceRectSize 对应）。")]
        [SerializeField]
        [Min(1e-2f)]
        private float referenceRectSize = 100f;

        [Tooltip("用于算世界直径的攻击半径基准，世界直径 = 2×该值（应与 ECS/表射程一致）。")]
        [SerializeField]
        [Min(1e-4f)]
        private float attackRadiusBaselineMeters = 250f;

        [Tooltip("(2 × 半径基准 / referenceRectSize) 之上的额外倍数，便于美术微调。")]
        [SerializeField]
        private float inspectorDiameterScaleMultiplier = 1f;

        [Header("Pose")]
        [Tooltip("略高于地面，减轻 Z-fighting。")]
        [SerializeField]
        private float heightOffset = 0.03f;

        private CanvasGroup _canvasGroup;

        private float _storedGraphicAlpha = 1f;

        private void Awake()
        {
            EnsureSetup();
            ApplyDiameterScaleToRoot();
            SetVisible(false);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (presentationRoot == null)
                presentationRoot = transform;

            EnsureSetup();
            ApplyDiameterScaleToRoot();
        }
#endif

        private void EnsureSetup()
        {
            if (presentationRoot == null)
                presentationRoot = transform;

            if (discImage != null && discImage.rectTransform != null)
            {
                discImage.raycastTarget = false;
                discImage.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, referenceRectSize);
                discImage.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, referenceRectSize);
                _storedGraphicAlpha = discImage.color.a;
            }

            _canvasGroup = presentationRoot.GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                _canvasGroup = presentationRoot.gameObject.AddComponent<CanvasGroup>();
                _canvasGroup.interactable = false;
                _canvasGroup.blocksRaycasts = false;
            }

            if (_canvasGroup != null)
            {
                _canvasGroup.interactable = false;
                _canvasGroup.blocksRaycasts = false;
            }

            WarnIfCanvasNotWorldSpace();
        }

        private void WarnIfCanvasNotWorldSpace()
        {
            var canvas = presentationRoot != null ? presentationRoot.GetComponentInChildren<Canvas>(true) : null;
            if (canvas != null && canvas.renderMode != RenderMode.WorldSpace)
                Debug.LogWarning(
                    $"[{nameof(AttackRangeWorldUiDiscPresenter)}] Canvas 建议使用 World Space。当前：{canvas.renderMode}。",
                    this);
        }

        /// <summary>
        /// 均匀 localScale：<c>(2 × attackRadiusBaselineMeters / referenceRectSize) × inspectorDiameterScaleMultiplier</c>。<br/>
        /// 对应世界空间中圆盘直径 <c>= 2 × attackRadiusBaselineMeters × inspectorDiameterScaleMultiplier</c>（在父节点 scale 为 1 的前提下）。
        /// </summary>
        private void ApplyDiameterScaleToRoot()
        {
            if (presentationRoot == null)
                return;

            float k = (2f * attackRadiusBaselineMeters) / Mathf.Max(1e-4f, referenceRectSize) *
                      inspectorDiameterScaleMultiplier;
            presentationRoot.localScale = new Vector3(k, k, k);
        }

        public void SetVisible(bool visible)
        {
            if (presentationRoot != null)
                presentationRoot.gameObject.SetActive(visible);
        }

        public void SetAlphaMultiplier(float alpha01)
        {
            if (_canvasGroup == null)
                EnsureSetup();

            float v = Mathf.Clamp01(alpha01) * _storedGraphicAlpha;
            if (_canvasGroup != null)
                _canvasGroup.alpha = v;
            else if (discImage != null)
            {
                var c = discImage.color;
                c.a = v;
                discImage.color = c;
            }
        }

        /// <summary>只更新贴地位置与透明度。</summary>
        public void SetWorldDisc(Vector3 worldGroundCenter, float alphaMultiplier = 1f)
        {
            if (presentationRoot == null || discImage == null)
            {
                Debug.LogWarning($"[{nameof(AttackRangeWorldUiDiscPresenter)}] 请指定 presentationRoot 与 discImage。", this);
                return;
            }

            presentationRoot.gameObject.SetActive(true);

            presentationRoot.SetPositionAndRotation(
                worldGroundCenter + Vector3.up * heightOffset,
                Quaternion.Euler(90f, 0f, 0f));

            SetAlphaMultiplier(alphaMultiplier);
        }
    }
}
