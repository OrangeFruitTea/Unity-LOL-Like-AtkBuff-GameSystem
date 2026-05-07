using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.Presentation.Interaction
{
    /// <summary>
    /// 轻量地面线框：<see cref="LineRenderer"/> 圆环 / 扇形边界（设计文档 §5.2 方案 A）。<br/>
    /// 技能圆与普攻圆共用本组件；点地短暂提示用 <see cref="GroundIndicatorRequest.Circle"/> + 短 <see cref="GroundIndicatorRequest.DurationSeconds"/>。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GroundWorldLineIndicator : MonoBehaviour
    {
        [SerializeField]
        [Min(8)]
        private int ringSegments = 56;

        [SerializeField]
        [Min(4)]
        private int sectorArcSegments = 32;

        private LineRenderer _line;
        private readonly List<Vector3> _buffer = new List<Vector3>(128);

        private GroundIndicatorRequest _active;
        private bool _hasActive;
        private float _expiryRealtime;

        private void Awake()
        {
            if (!TryGetComponent(out _line))
                _line = gameObject.AddComponent<LineRenderer>();
            _line.useWorldSpace = true;
            _line.textureMode = LineTextureMode.Stretch;
            _line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _line.receiveShadows = false;
            _line.loop = false;
            _line.enabled = false;
        }

        private void LateUpdate()
        {
            if (!_hasActive || !_active.UsesRealtimeExpiry)
                return;

            if (Time.realtimeSinceStartup >= _expiryRealtime)
                HideInternal();
        }

        /// <summary>与设计文档：<c>PushOrReplace(request)</c> 一致。</summary>
        public void PushOrReplace(in GroundIndicatorRequest request)
        {
            if (_hasActive && request.Priority < _active.Priority)
                return;

            ApplyInternal(in request);
        }

        /// <summary>若当前活动预设为 <paramref name="kind"/> 则隐藏。</summary>
        public void HidePreset(GroundPresentationPresetKind kind)
        {
            if (_hasActive && _active.PresetKind == kind)
                HideInternal();
        }

        /// <summary>清除全部地面线框。</summary>
        public void HideAll()
        {
            HideInternal();
        }

        public void PushMovePing(
            Vector3 centerWorld,
            float radius,
            Color color,
            float lineWidth = 0.08f,
            float seconds = 0.45f)
        {
            var req = GroundIndicatorRequest.Circle(
                GroundPresentationPresetKind.MoveClick,
                GroundPresentationPriority.MovePing,
                centerWorld,
                radius,
                color,
                lineWidth,
                seconds);
            PushOrReplace(in req);
        }

        public void PushAttackRangeCircle(
            Vector3 centerWorld,
            float radius,
            Color color,
            float lineWidth = 0.06f)
        {
            var req = GroundIndicatorRequest.Circle(
                GroundPresentationPresetKind.AttackRange,
                GroundPresentationPriority.AttackPreview,
                centerWorld,
                radius,
                color,
                lineWidth,
                float.MaxValue);
            PushOrReplace(in req);
        }

        public void PushSkillCircle(
            Vector3 centerWorld,
            float radius,
            Color color,
            float lineWidth = 0.07f)
        {
            var req = GroundIndicatorRequest.Circle(
                GroundPresentationPresetKind.SkillCircle,
                GroundPresentationPriority.SkillAim,
                centerWorld,
                radius,
                color,
                lineWidth,
                float.MaxValue);
            PushOrReplace(in req);
        }

        public void PushSkillSector(
            Vector3 centerWorld,
            float radius,
            Vector3 headingFlat,
            float sectorAngleDeg,
            Color color,
            float lineWidth = 0.07f)
        {
            var req = GroundIndicatorRequest.Sector(
                GroundPresentationPresetKind.SkillSector,
                GroundPresentationPriority.SkillAim,
                centerWorld,
                radius,
                headingFlat,
                sectorAngleDeg,
                color,
                lineWidth,
                float.MaxValue);
            PushOrReplace(in req);
        }

        private void ApplyInternal(in GroundIndicatorRequest request)
        {
            _active = request;
            _hasActive = true;
            _line.enabled = true;
            _line.startWidth = request.LineWidth;
            _line.endWidth = request.LineWidth;
            _line.startColor = request.Color;
            _line.endColor = request.Color;

            switch (request.PresetKind)
            {
                case GroundPresentationPresetKind.MoveClick:
                case GroundPresentationPresetKind.AttackRange:
                case GroundPresentationPresetKind.SkillCircle:
                    _line.loop = true;
                    BuildClosedRingXZ(request.Center, request.Radius, ringSegments);
                    break;
                case GroundPresentationPresetKind.SkillSector:
                    _line.loop = false;
                    BuildSectorPolylineXZ(
                        request.Center,
                        request.Radius,
                        request.HeadingFlatNormalized,
                        request.SectorAngleDeg,
                        sectorArcSegments);
                    break;
                default:
                    HideInternal();
                    return;
            }

            _line.positionCount = _buffer.Count;
            for (var i = 0; i < _buffer.Count; i++)
                _line.SetPosition(i, _buffer[i]);

            if (request.UsesRealtimeExpiry)
                _expiryRealtime = Time.realtimeSinceStartup + request.DurationSeconds;
        }

        private void HideInternal()
        {
            _hasActive = false;
            if (_line != null)
            {
                _line.enabled = false;
                _line.positionCount = 0;
            }

            _buffer.Clear();
        }

        private void BuildClosedRingXZ(Vector3 center, float radius, int segments)
        {
            _buffer.Clear();
            if (segments < 8)
                segments = 8;

            for (var i = 0; i < segments; i++)
            {
                var t = i / (float)segments * Mathf.PI * 2f;
                var x = Mathf.Cos(t) * radius;
                var z = Mathf.Sin(t) * radius;
                _buffer.Add(new Vector3(center.x + x, center.y, center.z + z));
            }
        }

        private void BuildSectorPolylineXZ(
            Vector3 center,
            float radius,
            Vector3 forwardFlatNormalized,
            float angleDeg,
            int arcSeg)
        {
            _buffer.Clear();
            arcSeg = Mathf.Max(arcSeg, 4);
            var halfDeg = angleDeg * 0.5f;

            Vector3 fwd = forwardFlatNormalized;
            fwd.y = 0f;
            fwd = fwd.sqrMagnitude < 1e-6f ? Vector3.forward : fwd.normalized;

            Vector3 RimPoint(float yawDegOffset)
            {
                var dir = Quaternion.AngleAxis(yawDegOffset, Vector3.up) * fwd;
                var p = center + dir * radius;
                p.y = center.y;
                return p;
            }

            var left = RimPoint(-halfDeg);
            var right = RimPoint(halfDeg);

            _buffer.Add(center);
            _buffer.Add(left);

            var innerSegments = Mathf.Max(arcSeg, 4);
            for (var i = 1; i < innerSegments - 1; i++)
            {
                var t = i / (float)(innerSegments - 1);
                var yaw = Mathf.Lerp(-halfDeg, halfDeg, t);
                var p = RimPoint(yaw);
                _buffer.Add(p);
            }

            _buffer.Add(right);
            _buffer.Add(center);
        }
    }
}
