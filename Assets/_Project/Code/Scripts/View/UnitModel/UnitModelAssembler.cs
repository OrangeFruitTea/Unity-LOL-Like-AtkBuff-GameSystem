using UnityEngine;
using UnityEngine.Serialization;

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

        [FormerlySerializedAs("animatorControllerOverride")]
        [SerializeField]
        [Tooltip("玩法用 Animator Controller（.controller 资产）。FBX 预制里的 Animator 通常不带 Controller，须在此指定；可与多个角色预制共用同一套 Controller。")]
        private RuntimeAnimatorController animatorController;

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

            // 父物体 inactive 时，子物体 Awake 会延后到重新激活之后；这样可先挂上 Controller，再跑 Animator / 其它脚本的 Awake。
            var anchorGo = modelAnchor.gameObject;
            var anchorWasActive = anchorGo.activeSelf;
            if (anchorWasActive)
                anchorGo.SetActive(false);

            try
            {
                _spawnedModelRoot = Instantiate(modelPrefab, modelAnchor);
                _spawnedModelRoot.name = modelPrefab.name;
                _spawnedModelRoot.transform.localPosition = Vector3.zero;
                _spawnedModelRoot.transform.localRotation = Quaternion.identity;
                _spawnedModelRoot.transform.localScale = Vector3.one;

                ApplyAnimatorController(_spawnedModelRoot);
                WarnIfAnimatorsStillHaveNoController(_spawnedModelRoot);
            }
            finally
            {
                if (anchorWasActive)
                    anchorGo.SetActive(true);
            }
        }

        private void ApplyAnimatorController(GameObject modelRoot)
        {
            if (animatorController == null)
                return;

            var animators = modelRoot.GetComponentsInChildren<Animator>(true);
            if (animators.Length == 0)
                return;

            foreach (var anim in animators)
            {
                anim.runtimeAnimatorController = animatorController;
                anim.Rebind();
                anim.Update(0f);
            }
        }

        private void WarnIfAnimatorsStillHaveNoController(GameObject modelRoot)
        {
            if (modelRoot == null)
                return;

            foreach (var anim in modelRoot.GetComponentsInChildren<Animator>(true))
            {
                if (anim.runtimeAnimatorController != null)
                    continue;

                Debug.LogWarning(
                    $"[{nameof(UnitModelAssembler)}] '{name}' 已实例化模型「{modelPrefab.name}」，但 Animator 仍无 Controller。" +
                    $"请在 Inspector 的「{nameof(animatorController)}」槽拖入 .controller（FBX 导入预制默认不带 gameplay Controller）。",
                    this);
                return;
            }
        }
    }
}
