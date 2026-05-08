using UnityEngine;

namespace Gameplay.Presentation.Interaction
{
    public enum WorldHudBillboardMode
    {
        /// <summary>与摄像机视平面平行（同 <see cref="Camera.transform.rotation"/>），头顶 UI 常用。</summary>
        FaceCamera,

        /// <summary>仅绕 Y 轴朝向相机在地面上的投影，俯视角更稳。</summary>
        YawTowardCamera
    }

    /// <summary>World Space 血条/名牌根节点朝向（设计文档 §4.2）。</summary>
    [DefaultExecutionOrder(32000)]
    [DisallowMultipleComponent]
    public sealed class WorldBillboardHudPresenter : MonoBehaviour
    {
        [Tooltip("留空则每帧使用当前可用的主渲染相机（例如玩家生成后替换的 MainCamera）；拖入则固定面向该相机。")]
        [SerializeField]
        private Camera targetCamera;

        [SerializeField]
        private WorldHudBillboardMode mode = WorldHudBillboardMode.YawTowardCamera;

        private bool _loggedMissingCamera;

        private void LateUpdate()
        {
            var cam = ResolveRenderingCamera();
            if (cam == null)
            {
                if (!_loggedMissingCamera)
                {
                    _loggedMissingCamera = true;
                    Debug.LogWarning(
                        $"[{nameof(WorldBillboardHudPresenter)}] No usable camera on '{name}' — billboard rotation skipped. " +
                        "Leave Target Camera empty to track runtime MainCamera, or assign the follow camera explicitly.",
                        this);
                }

                return;
            }

            switch (mode)
            {
                case WorldHudBillboardMode.FaceCamera:
                    transform.rotation = cam.transform.rotation;
                    break;
                default:
                {
                    var camPos = cam.transform.position;
                    var here = transform.position;
                    var toCam = camPos - here;
                    toCam.y = 0f;
                    if (toCam.sqrMagnitude < 1e-8f)
                        return;
                    transform.rotation = Quaternion.LookRotation(toCam.normalized, Vector3.up);
                    break;
                }
            }
        }

        /// <summary>
        /// 显式 <see cref="targetCamera"/> 优先；否则<b>不缓存</b> <see cref="Camera.main"/>，避免首帧场景相机被钉死、忽略后续动态主相机。
        /// </summary>
        private Camera ResolveRenderingCamera()
        {
            if (targetCamera != null && targetCamera.isActiveAndEnabled)
                return targetCamera;

            var main = Camera.main;
            if (main != null && main.isActiveAndEnabled)
                return main;

            var tagged = GameObject.FindGameObjectsWithTag("MainCamera");
            for (var i = 0; i < tagged.Length; i++)
            {
                if (tagged[i] == null || !tagged[i].activeInHierarchy)
                    continue;
                var c = tagged[i].GetComponent<Camera>();
                if (c != null && c.isActiveAndEnabled)
                    return c;
            }

#if UNITY_2022_3_OR_NEWER
            return Object.FindFirstObjectByType<Camera>(FindObjectsInactive.Exclude);
#else
            return Object.FindObjectOfType<Camera>();
#endif
        }
    }
}
