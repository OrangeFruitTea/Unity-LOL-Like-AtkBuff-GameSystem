using UnityEngine;

namespace Gameplay.Presentation.Interaction
{
    public enum GroundPresentationPresetKind : byte
    {
        MoveClick = 1,
        AttackRange = 2,
        SkillCircle = 3,
        SkillSector = 4
    }

    public enum GroundPresentationPriority : byte
    {
        MovePing = 0,
        AttackPreview = 1,
        SkillAim = 2
    }

    /// <summary>地面线框单次请求（设计文档 §5.1）。</summary>
    public readonly struct GroundIndicatorRequest
    {
        public GroundIndicatorRequest(
            GroundPresentationPresetKind presetKind,
            GroundPresentationPriority priority,
            Vector3 center,
            float radius,
            Color color,
            float lineWidth,
            float durationSeconds,
            Vector3 headingFlatNormalized,
            float sectorAngleDeg)
        {
            PresetKind = presetKind;
            Priority = priority;
            Center = center;
            Radius = radius;
            Color = color;
            LineWidth = lineWidth;
            DurationSeconds = durationSeconds;
            HeadingFlatNormalized = headingFlatNormalized;
            SectorAngleDeg = sectorAngleDeg;
        }

        public GroundPresentationPresetKind PresetKind { get; }
        public GroundPresentationPriority Priority { get; }
        public Vector3 Center { get; }
        public float Radius { get; }
        public Color Color { get; }
        public float LineWidth { get; }
        /// <summary><see cref="float.MaxValue"/> 表示持续到 <c>Hide</c>。</summary>
        public float DurationSeconds { get; }
        public Vector3 HeadingFlatNormalized { get; }
        public float SectorAngleDeg { get; }

        public bool UsesRealtimeExpiry => DurationSeconds > 0f && DurationSeconds < float.MaxValue * 0.99f;

        public static GroundIndicatorRequest Circle(
            GroundPresentationPresetKind presetKind,
            GroundPresentationPriority priority,
            Vector3 center,
            float radius,
            Color color,
            float lineWidth,
            float durationSeconds)
        {
            return new GroundIndicatorRequest(
                presetKind,
                priority,
                center,
                radius,
                color,
                lineWidth,
                durationSeconds,
                Vector3.forward,
                0f);
        }

        public static GroundIndicatorRequest Sector(
            GroundPresentationPresetKind presetKind,
            GroundPresentationPriority priority,
            Vector3 center,
            float radius,
            Vector3 headingFlat,
            float sectorAngleDeg,
            Color color,
            float lineWidth,
            float durationSeconds)
        {
            headingFlat.y = 0f;
            if (headingFlat.sqrMagnitude < 1e-6f)
                headingFlat = Vector3.forward;
            else
                headingFlat.Normalize();

            return new GroundIndicatorRequest(
                presetKind,
                priority,
                center,
                radius,
                color,
                lineWidth,
                durationSeconds,
                headingFlat,
                sectorAngleDeg);
        }

        /// <summary>由预设推导默认优先级（与设计文档 §5.3）一致。</summary>
        public static GroundPresentationPriority DefaultPriority(GroundPresentationPresetKind kind)
        {
            switch (kind)
            {
                case GroundPresentationPresetKind.MoveClick:
                    return GroundPresentationPriority.MovePing;
                case GroundPresentationPresetKind.AttackRange:
                    return GroundPresentationPriority.AttackPreview;
                default:
                    return GroundPresentationPriority.SkillAim;
            }
        }
    }
}
