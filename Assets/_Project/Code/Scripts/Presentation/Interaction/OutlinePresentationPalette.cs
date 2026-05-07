using UnityEngine;

namespace Gameplay.Presentation.Interaction
{
    /// <summary>悬停 / 选中 Rim 颜色与强度；可创建为资产挂到 <see cref="RimOutlineDriver"/>。</summary>
    [CreateAssetMenu(fileName = "OutlinePresentationPalette", menuName = "Gameplay/Presentation/Outline Palette")]
    public sealed class OutlinePresentationPalette : ScriptableObject
    {
        [SerializeField]
        private Color idleRimColor = new Color(1f, 1f, 1f, 1f);

        [SerializeField]
        [Min(0f)]
        private float idleRimStrength;

        [SerializeField]
        private Color hoverRimColor = new Color(0.4f, 0.85f, 1f, 1f);

        [SerializeField]
        [Min(0f)]
        private float hoverRimStrength = 0.35f;

        [SerializeField]
        private Color selectedRimColor = new Color(1f, 0.85f, 0.2f, 1f);

        [SerializeField]
        [Min(0f)]
        private float selectedRimStrength = 0.65f;

        [SerializeField]
        [Min(0.01f)]
        private float rimPower = 3f;

        public Color IdleRimColor => idleRimColor;
        public float IdleRimStrength => idleRimStrength;
        public Color HoverRimColor => hoverRimColor;
        public float HoverRimStrength => hoverRimStrength;
        public Color SelectedRimColor => selectedRimColor;
        public float SelectedRimStrength => selectedRimStrength;
        public float RimPower => rimPower;
    }
}
