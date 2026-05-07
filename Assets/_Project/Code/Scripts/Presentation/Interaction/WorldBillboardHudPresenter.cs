using UnityEngine;

namespace Gameplay.Presentation.Interaction
{
    public enum WorldHudBillboardMode
    {
        /// <summary>始终面向相机（适合斜视角镜头）。</summary>
        FaceCamera,

        /// <summary>仅绕 Y 轴朝向相机，俯视角更稳。</summary>
        YawTowardCamera
    }

    /// <summary>World Space 血条/名牌根节点朝向（设计文档 §4.2）。</summary>
    [DisallowMultipleComponent]
    public sealed class WorldBillboardHudPresenter : MonoBehaviour
    {
        [SerializeField]
        private Camera targetCamera;

        [SerializeField]
        private WorldHudBillboardMode mode = WorldHudBillboardMode.YawTowardCamera;

        private void LateUpdate()
        {
            if (targetCamera == null)
                targetCamera = Camera.main;
            if (targetCamera == null)
                return;

            var camPos = targetCamera.transform.position;
            var here = transform.position;

            switch (mode)
            {
                case WorldHudBillboardMode.FaceCamera:
                {
                    var toCam = camPos - here;
                    if (toCam.sqrMagnitude < 1e-8f)
                        return;
                    transform.rotation = Quaternion.LookRotation(toCam.normalized, Vector3.up);
                    break;
                }
                default:
                {
                    var toCam = camPos - here;
                    toCam.y = 0f;
                    if (toCam.sqrMagnitude < 1e-8f)
                        return;
                    transform.rotation = Quaternion.LookRotation(toCam.normalized, Vector3.up);
                    break;
                }
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (targetCamera == null)
                targetCamera = Camera.main;
        }
#endif
    }
}
