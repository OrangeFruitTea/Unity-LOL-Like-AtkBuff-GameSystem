using UnityEngine;

namespace Basement.MatchTime
{
    /// <summary>
    /// 在 Unity 更新循环中驱动 <see cref="MatchTimeService.Instance"/>；可置于常驻对象上。
    /// </summary>
    public sealed class MatchTimeUnityDriver : MonoBehaviour
    {
        private static MatchTimeUnityDriver _instance;

        [SerializeField] private bool _dontDestroyOnLoad = true;

        /// <summary> 全局对局时间控制；不依赖本组件是否存在于场景中。 </summary>
        public static IMatchTimeControl ActiveControl => MatchTimeService.Instance;

        /// <summary> 只读视图，与 <see cref="ActiveControl"/> 指向同一实例。 </summary>
        public static IMatchTime Active => MatchTimeService.Instance;

        public IMatchTimeControl Control => MatchTimeService.Instance;

        public IMatchTime ReadOnly => MatchTimeService.Instance;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;

            if (_dontDestroyOnLoad && Application.isPlaying)
                DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        private void Update()
        {
            MatchTimeService.Instance.TickUpdate();
        }

        private void FixedUpdate()
        {
            MatchTimeService.Instance.TickFixedUpdate();
        }
    }
}
