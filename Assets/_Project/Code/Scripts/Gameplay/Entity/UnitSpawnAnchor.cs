using UnityEngine;

namespace Core.Entity
{
    /// <summary>
    /// 记录单位出生点（世界坐标）；复活时 <see cref="UnitDeathRespawnBehaviour"/> 会将宿主传送回此处。<br/>
    /// 运行时生成单位可在摆放完毕后调用 <see cref="CaptureFromCurrentTransform"/>。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UnitSpawnAnchor : MonoBehaviour
    {
        [Tooltip("Start 时抓取当前 transform 作为出生点（ Instantiate 后已在正确世界坐标时可用）。")]
        [SerializeField]
        private bool captureSpawnAtStart = true;

        [SerializeField]
        private Vector3 spawnWorldPosition;

        [SerializeField]
        private Quaternion spawnWorldRotation = Quaternion.identity;

        public Vector3 SpawnWorldPosition => spawnWorldPosition;

        public Quaternion SpawnWorldRotation => spawnWorldRotation;

        private void Start()
        {
            if (captureSpawnAtStart)
                CaptureFromCurrentTransform();
        }

        /// <summary>用当前场景坐标覆盖出生点（SpawnSystem 摆放完成后也可调用）。</summary>
        public void CaptureFromCurrentTransform()
        {
            spawnWorldPosition = transform.position;
            spawnWorldRotation = transform.rotation;
        }

        /// <summary>手动指定泉水 / 营地锚点（不跟随角色当前位移）。</summary>
        public void SetSpawnWorldPose(Vector3 worldPosition, Quaternion worldRotation)
        {
            spawnWorldPosition = worldPosition;
            spawnWorldRotation = worldRotation;
        }
    }
}
