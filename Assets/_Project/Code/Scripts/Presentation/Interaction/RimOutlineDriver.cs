using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.Presentation.Interaction
{
    /// <summary>
    /// 单位侧 Rim 反馈：与设计文档 §3 一致，使用单份 <see cref="MaterialPropertyBlock"/> 驱动子节点 <see cref="Renderer"/>。
    /// <see cref="PresentationSelectionHub"/> 在 Hover/Selected 变化时调用 <see cref="PresentationRimVisualKind"/> 同步。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RimOutlineDriver : MonoBehaviour
    {
        private static readonly List<RimOutlineDriver> Registry = new List<RimOutlineDriver>(64);

        [SerializeField]
        private OutlinePresentationPalette palette;

        [SerializeField]
        private bool includeInactiveRenderers;

        [Tooltip("仅处理名称包含该后缀的 Renderer；留空表示处理全部。")]
        [SerializeField]
        private string rendererNameSuffixInclude;

        private readonly MaterialPropertyBlock _mpb = new MaterialPropertyBlock();

        [SerializeField]
        private Renderer[] explicitRenderers;

        private Renderer[] _resolvedRenderers;
        private PresentationRimVisualKind _pendingKind = PresentationRimVisualKind.Idle;

        public Transform PresentationRoot => transform;

        /// <summary>可由场景单例 <see cref="PresentationSelectionHub"/> 或测试代码直接驱动。</summary>
        public PresentationRimVisualKind CurrentKind { get; private set; } = PresentationRimVisualKind.Idle;

        private void OnEnable()
        {
            if (!Registry.Contains(this))
                Registry.Add(this);
            BakeRenderersIfNeeded();
            if (PresentationSelectionHub.Instance != null)
                PresentationSelectionHub.Instance.RefreshRimVisuals();
            else
                Apply(CurrentKind);
        }

        private void OnDisable()
        {
            Apply(PresentationRimVisualKind.Idle);
            Registry.Remove(this);
        }

        private void BakeRenderersIfNeeded()
        {
            if (explicitRenderers is { Length: > 0 })
            {
                _resolvedRenderers = explicitRenderers;
                return;
            }

            {
                var list = new List<Renderer>(16);
                GetComponentsInChildren(includeInactiveRenderers, list);
                _resolvedRenderers = list.ToArray();
            }

            if (string.IsNullOrEmpty(rendererNameSuffixInclude))
                return;

            var suffix = rendererNameSuffixInclude;
            var filtered = new List<Renderer>(_resolvedRenderers.Length);
            for (var i = 0; i < _resolvedRenderers.Length; i++)
            {
                var r = _resolvedRenderers[i];
                if (r == null)
                    continue;
                if (r.name.EndsWith(suffix, System.StringComparison.Ordinal))
                    filtered.Add(r);
            }

            _resolvedRenderers = filtered.ToArray();
        }

        /// <summary>
        /// 选中优先于悬停（设计文档 §3.2「保持选中配色」）：由协调方计算后传入。
        /// </summary>
        public void Apply(PresentationRimVisualKind kind)
        {
            CurrentKind = kind;
            _pendingKind = kind;
            PushToRenderers(kind);
        }

        public void ApplyPendingFromSelection(Transform hoverRoot, Transform selectedRoot)
        {
            var root = PresentationRoot;
            var isSelected = ReferenceEquals(selectedRoot, root);
            var isHover = ReferenceEquals(hoverRoot, root);
            var kind = PresentationRimVisualPlanner.Resolve(isSelected, isHover);
            Apply(kind);
        }

        private void PushToRenderers(PresentationRimVisualKind kind)
        {
            Color color;
            float strength;
            float power;
            if (palette == null)
            {
                color = kind == PresentationRimVisualKind.Idle ? Color.black : Color.white;
                strength = kind == PresentationRimVisualKind.Idle ? 0f : 0.4f;
                power = 3f;
            }
            else
            {
                switch (kind)
                {
                    case PresentationRimVisualKind.HoverOnly:
                        color = palette.HoverRimColor;
                        strength = palette.HoverRimStrength;
                        break;
                    case PresentationRimVisualKind.Selected:
                        color = palette.SelectedRimColor;
                        strength = palette.SelectedRimStrength;
                        break;
                    default:
                        color = palette.IdleRimColor;
                        strength = palette.IdleRimStrength;
                        break;
                }

                power = palette.RimPower;
            }

            ApplyRimParams(color, strength, power);
        }

        private void ApplyRimParams(Color color, float strength, float power)
        {
            if (_resolvedRenderers == null)
                BakeRenderersIfNeeded();

            for (var i = 0; i < _resolvedRenderers.Length; i++)
            {
                var renderer = _resolvedRenderers[i];
                if (renderer == null)
                    continue;
                renderer.GetPropertyBlock(_mpb);
                _mpb.SetColor(RimPresentationShaderIds.RimColor, color);
                _mpb.SetFloat(RimPresentationShaderIds.RimStrength, strength);
                _mpb.SetFloat(RimPresentationShaderIds.RimPower, power);
                renderer.SetPropertyBlock(_mpb);
            }
        }

        /// <summary>由 <see cref="PresentationSelectionHub"/> 在 Hover/Selected 变化后调用。</summary>
        public static void SyncAll(Transform hoverRoot, Transform selectedRoot)
        {
            for (var i = 0; i < Registry.Count; i++)
                Registry[i].ApplyPendingFromSelection(hoverRoot, selectedRoot);
        }
    }

    public enum PresentationRimVisualKind : byte
    {
        Idle = 0,
        HoverOnly = 1,
        Selected = 2
    }

    public static class PresentationRimVisualPlanner
    {
        public static PresentationRimVisualKind Resolve(bool isSelected, bool isHover)
        {
            if (isSelected)
                return PresentationRimVisualKind.Selected;
            if (isHover)
                return PresentationRimVisualKind.HoverOnly;
            return PresentationRimVisualKind.Idle;
        }
    }
}
