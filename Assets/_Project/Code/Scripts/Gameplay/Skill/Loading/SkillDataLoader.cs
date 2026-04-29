using System.IO;
using Basement.Json;
using Gameplay.Skill.Config;
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
            string full = Path.Combine(Application.streamingAssetsPath, _relativePath);
            if (!File.Exists(full))
            {
                Debug.LogWarning($"[SkillDataLoader] 未找到技能表: {full}，使用空目录。");
                SkillCatalog.ReplaceAll(System.Array.Empty<SkillDefinition>());
                return;
            }

            if (JsonManager.Instance == null)
            {
                Debug.LogError("[SkillDataLoader] JsonManager 未初始化，无法解析技能表。");
                return;
            }

            var result = JsonManager.Instance.DeserializeFromFilePath<SkillDataFileDto>(full, JsonSerializerProfile.GameContent);
            if (!result.Success)
            {
                Debug.LogError($"[SkillDataLoader] 解析失败: {result.Error}");
                return;
            }

            var dto = result.Value;
            SkillCatalog.ReplaceAll(dto.Skills ?? System.Array.Empty<SkillDefinition>());
            Debug.Log($"[SkillDataLoader] 已加载技能 {dto.Skills?.Count ?? 0} 条 (schema {dto.SchemaVersion})");
        }
    }
}
