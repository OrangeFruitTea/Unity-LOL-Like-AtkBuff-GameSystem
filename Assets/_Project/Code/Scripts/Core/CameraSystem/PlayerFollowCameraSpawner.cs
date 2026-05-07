using Core.Entity;
using UnityEngine;

namespace Core.CameraSystem
{
    /// <summary>
    /// 玩家生成时动态创建 <see cref="CameraController"/> 与 <see cref="Camera"/>，并打上 MainCamera 标签，
    /// 使 <see cref="Camera.main"/>、点击寻路等逻辑无需依赖场景里预摆的相机。
    /// </summary>
    public static class PlayerFollowCameraSpawner
    {
        /// <summary>
        /// 创建跟随相机并绑定玩家根节点。应在玩家实体已就位、ECS Flush 成功后调用。<br/>
        /// 为避免 <see cref="CameraController"/> 在 <see cref="MonoBehaviour.OnEnable"/> 里误订阅
        /// <see cref="TestPlayerSpawner"/>，先在 <b>未激活</b> 物体上装配再激活。
        /// </summary>
        /// <param name="playerRoot">跟随目标（通常为玩家 <see cref="EntityBase"/> 根 Transform）。</param>
        /// <param name="retireExistingMainCameraRigs">为 true 时禁用场景中已有 MainCamera 的 Camera/AudioListener 并取消其 MainCamera Tag。</param>
        /// <param name="cameraDepth">新开 Camera.depth；默认同常为 -1。</param>
        public static CameraController CreateForPlayer(
            Transform playerRoot,
            bool retireExistingMainCameraRigs = true,
            int cameraDepth = -1)
        {
            if (playerRoot == null)
            {
                Debug.LogWarning("[PlayerFollowCameraSpawner] playerRoot is null.");
                return null;
            }

            if (retireExistingMainCameraRigs)
                RetireExistingTaggedMainCameras();

            var go = new GameObject("PlayerFollowCamera_Dynamic");
            go.SetActive(false);
            go.tag = "MainCamera";

            var cam = go.AddComponent<Camera>();
            cam.enabled = true;
            cam.depth = cameraDepth;

            go.AddComponent<AudioListener>();

            var ctrl = go.AddComponent<CameraController>();
            ctrl.InitializeForDedicatedPlayerFollow(playerRoot);

            go.SetActive(true);
            Debug.Log($"[PlayerFollowCameraSpawner] Main camera created and bound to '{playerRoot.name}'.");
            return ctrl;
        }

        /// <summary>禁用场景中当前带 MainCamera 标签的物体（整块关活），避免出现多个主相机与残留脚本 Update。</summary>
        public static void RetireExistingTaggedMainCameras()
        {
            var tagged = GameObject.FindGameObjectsWithTag("MainCamera");
            foreach (var go in tagged)
            {
                if (go == null)
                    continue;
                go.tag = "Untagged";
                go.SetActive(false);
            }
        }
    }
}
