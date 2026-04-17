using UnityEngine;

namespace Basement.MatchTime
{
    /// <summary>
    /// 对局时间服务：薄封装 Unity 时间并维护 <see cref="MatchElapsed"/>。
    /// 需由 <see cref="MatchTimeUnityDriver"/> 或等价逻辑每帧调用 <see cref="TickUpdate"/> / <see cref="TickFixedUpdate"/>。
    /// </summary>
    public sealed class MatchTimeService : IMatchTimeControl
    {
        private static MatchTimeService _instance;
        private static readonly object Gate = new object();

        public static MatchTimeService Instance
        {
            get
            {
                lock (Gate)
                {
                    return _instance ??= new MatchTimeService();
                }
            }
        }

        private bool _isMatchActive;
        private bool _paused;
        private float _matchElapsed;
        private float _deltaTime;
        private float _fixedDeltaTime;
        private float _unityScaledTime;
        private int _lastUpdateFrame = -1;
        private float _lastFixedTimeRecorded = -1f;

        private MatchTimeService()
        {
        }

        public bool IsMatchActive => _isMatchActive;

        public bool IsMatchPaused => _paused;

        public float MatchElapsed => _matchElapsed;

        public float DeltaTime => _deltaTime;

        public float FixedDeltaTime => _fixedDeltaTime > 0f ? _fixedDeltaTime : Time.fixedDeltaTime;

        public float UnityScaledTime => _unityScaledTime;

        public void BeginMatch()
        {
            _isMatchActive = true;
            _paused = false;
            _matchElapsed = 0f;
        }

        public void EndMatch()
        {
            _isMatchActive = false;
            _paused = false;
            _matchElapsed = 0f;
        }

        public void PauseMatch()
        {
            if (_isMatchActive)
                _paused = true;
        }

        public void ResumeMatch()
        {
            if (_isMatchActive)
                _paused = false;
        }

        /// <summary> 在 <c>Update</c> 中调用；同一帧多次调用仅生效一次。 </summary>
        public void TickUpdate()
        {
            if (Time.frameCount == _lastUpdateFrame)
                return;
            _lastUpdateFrame = Time.frameCount;

            _deltaTime = Time.deltaTime;
            _unityScaledTime = Time.time;
            if (_isMatchActive && !_paused)
                _matchElapsed += _deltaTime;
        }

        /// <summary> 在 <c>FixedUpdate</c> 中调用；同一物理步多次调用仅生效一次。 </summary>
        public void TickFixedUpdate()
        {
            if (Mathf.Approximately(Time.fixedTime, _lastFixedTimeRecorded))
                return;
            _lastFixedTimeRecorded = Time.fixedTime;

            _fixedDeltaTime = Time.fixedDeltaTime;
        }
    }
}
