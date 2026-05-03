using Core.Entity;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Tooltip("手动指定跟随目标；留空且开启下方自动绑定时，将使用 TestPlayerSpawner 生成的单位。")]
    public Transform player;

    [Tooltip("Player 为空时，订阅 TestPlayerSpawner 生成完成并绑定为跟随目标。")]
    [SerializeField] private bool autoBindTestPlayerWhenPlayerEmpty = true;

    [Header("自动绑定时的一次性取景（使玩家落在视野中心附近）")]
    [Tooltip("生成回调里将相机瞬移到 player.position + 该世界空间偏移，再可选 LookAt。")]
    [SerializeField]
    private Vector3 autoBindWorldOffsetFromPlayer = new Vector3(0f, 14f, -10f);

    [Tooltip("瞬移后是否 LookAt 玩家近似中心（俯视角建议开启）。")]
    [SerializeField]
    private bool snapLookAtPlayerOnAutoBind = true;

    [Tooltip("相对玩家的注视点（通常为身高/胸部：0,1,0），避免盯着脚底。")]
    [SerializeField]
    private Vector3 lookAtFocusPointOffset = new Vector3(0f, 1f, 0f);

    private Vector3 _cameraOffset;
    private bool _offsetInitialized;

    [Range(0.01f, 1.0f)] public float smoothness = 0.5f;

    private void OnEnable()
    {
        if (!autoBindTestPlayerWhenPlayerEmpty)
            return;

        TestPlayerSpawner.TestPlayerSpawned += OnTestPlayerSpawned;
        if (player == null && TestPlayerSpawner.LastSpawnedPlayerRoot != null)
            BindPlayer(TestPlayerSpawner.LastSpawnedPlayerRoot);
    }

    private void OnDisable()
    {
        TestPlayerSpawner.TestPlayerSpawned -= OnTestPlayerSpawned;
    }

    private void Start()
    {
        TryInitializeOffsetForManualPlayer();
    }

    private void OnTestPlayerSpawned(Transform spawnedRoot)
    {
        if (!autoBindTestPlayerWhenPlayerEmpty)
            return;
        if (player != null)
            return;

        BindPlayer(spawnedRoot);
    }

    private void BindPlayer(Transform spawnedRoot)
    {
        if (spawnedRoot == null)
            return;

        player = spawnedRoot;
        ApplyAutoBindInitialFraming();
        Debug.Log($"[CameraController] 已自动绑定并取景: {spawnedRoot.name}（TestPlayerSpawner）。");
    }

    /// <summary>TestPlayer 生成：先摆相机再记下跟随偏移，避免沿用场景里错误的初始机位。</summary>
    private void ApplyAutoBindInitialFraming()
    {
        if (player == null)
            return;

        transform.position = player.position + autoBindWorldOffsetFromPlayer;
        if (snapLookAtPlayerOnAutoBind)
            transform.LookAt(player.position + lookAtFocusPointOffset, Vector3.up);

        _cameraOffset = transform.position - player.position;
        _offsetInitialized = true;
    }

    /// <summary>Inspector 已拖好 Player 时：保留场景机位与目标的相对关系。</summary>
    private void TryInitializeOffsetForManualPlayer()
    {
        if (player == null || _offsetInitialized)
            return;

        _cameraOffset = transform.position - player.position;
        _offsetInitialized = true;
    }

    private void Update()
    {
        if (player == null)
            return;

        if (!_offsetInitialized)
            TryInitializeOffsetForManualPlayer();

        Vector3 newPos = player.position + _cameraOffset;
        transform.position = Vector3.Slerp(transform.position, newPos, smoothness);
    }
}
