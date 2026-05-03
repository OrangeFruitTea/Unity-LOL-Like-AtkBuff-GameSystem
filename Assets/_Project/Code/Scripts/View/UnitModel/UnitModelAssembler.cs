using UnityEngine;

namespace View.UnitModel
{
    /// <summary>
    /// 在单位根仅保留引用：运行时把模型预制（FBX/Prefab）实例化到 <c>ModelRoot</c> 锚点下，避免父预制内嵌整套美术层级。<br/>
    /// 早于默认脚本执行，便于同物体上的 <see cref="Gameplay.Presentation.UnitAnimDrv"/> 在 <c>Awake</c> 中 <c>GetComponentInChildren&lt;Animator&gt;</c>。
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-50)]
    public sealed class UnitModelAssembler : MonoBehaviour
    {
        private const string DefaultModelAnchorChildName = "ModelRoot";

        [SerializeField]
        [Tooltip("模型实例的父节点；留空则在子级查找名为 ModelRoot 的物体，不存在则自动创建")]
        private Transform modelAnchor;

        [SerializeField]
        [Tooltip("角色模型预制根（如 Kenney BlockyCharacters 的 character-x FBX）")]
        private GameObject modelPrefab;

        [SerializeField]
        [Tooltip("非空时覆盖实例上的 Animator.runtimeAnimatorController")]
        private RuntimeAnimatorController animatorControllerOverride;

        [SerializeField]
        [Tooltip("生成前清空锚点下现有子物体（避免重复 Awake 叠加）")]
        private bool clearModelChildrenBeforeSpawn = true;

        private GameObject _spawnedModelRoot;

        private void Awake()
        {
            ResolveModelAnchor();
            SpawnModelIfConfigured();
        }

        private void OnDestroy()
        {
            if (_spawnedModelRoot != null)
            {
                Destroy(_spawnedModelRoot);
                _spawnedModelRoot = null;
            }
        }

        private void ResolveModelAnchor()
        {
            if (modelAnchor != null)
                return;

            var existing = transform.Find(DefaultModelAnchorChildName);
            if (existing != null)
            {
                modelAnchor = existing;
                return;
            }

            var go = new GameObject(DefaultModelAnchorChildName);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            modelAnchor = go.transform;
        }

        private void SpawnModelIfConfigured()
        {
            if (modelPrefab == null || modelAnchor == null)
                return;

            if (clearModelChildrenBeforeSpawn)
            {
                for (var i = modelAnchor.childCount - 1; i >= 0; i--)
                    DestroyImmediate(modelAnchor.GetChild(i).gameObject);
            }

            _spawnedModelRoot = Instantiate(modelPrefab, modelAnchor);
            _spawnedModelRoot.name = modelPrefab.name;
            _spawnedModelRoot.transform.localPosition = Vector3.zero;
            _spawnedModelRoot.transform.localRotation = Quaternion.identity;
            _spawnedModelRoot.transform.localScale = Vector3.one;

            if (animatorControllerOverride != null)
            {
                var anim = _spawnedModelRoot.GetComponentInChildren<Animator>(true);
                if (anim != null)
                    anim.runtimeAnimatorController = animatorControllerOverride;
            }
        }
    }
}
