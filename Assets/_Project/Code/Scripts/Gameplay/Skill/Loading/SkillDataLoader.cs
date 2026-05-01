using Core.ECS;
using UnityEngine;

namespace Gameplay.Skill.Loading
{
    /// <summary>
    /// 从 StreamingAssets 加载 <c>SkillData.json</c> 并填充 <see cref="SkillCatalog"/>。
    /// </summary>
    public sealed class SkillDataLoader : MonoBehaviour
    {
        public const string DefaultRelativePath = "SkillData.json";

        public static SkillDataLoader Instance { get; private set; }

        [SerializeField] private string _relativePath = DefaultRelativePath;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            Load();
        }

        public void Load()
        {
            if (!EcsWorld.TryReloadSkillCatalogFromStreaming(_relativePath, out var err) &&
                !string.IsNullOrEmpty(err))
                Debug.LogWarning($"[SkillDataLoader] {err}");
        }
    }
}
