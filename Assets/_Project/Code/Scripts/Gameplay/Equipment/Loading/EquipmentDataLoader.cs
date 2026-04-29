using System.IO;
using Basement.Json;
using Gameplay.Equipment.Config;
using Gameplay.Shop;
using UnityEngine;

namespace Gameplay.Equipment.Loading
{
    /// <summary>
    /// 从 StreamingAssets 加载 <c>EquipmentData.json</c> 并填充 <see cref="EquipmentCatalog"/>。
    /// 挂接到场景（与 <see cref="Gameplay.Skill.Loading.SkillDataLoader"/> 同类）。
    /// </summary>
    public sealed class EquipmentDataLoader : MonoBehaviour
    {
        public const string DefaultRelativePath = "EquipmentData.json";

        public static EquipmentDataLoader Instance { get; private set; }

        [SerializeField] private string _relativePath = DefaultRelativePath;

        [SerializeField] private bool _validateBuffDataAfterLoad = true;

        [SerializeField] private bool _validateBuffRegistryAfterLoad = true;

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
                Debug.LogWarning($"[EquipmentDataLoader] 未找到装备表: {full}，使用空目录。");
                EquipmentCatalog.ReplaceAll(System.Array.Empty<ItemConfigDefinition>());
                CraftRecipeCatalog.RebuildFrom(null);
                ShopCatalog.RebuildFrom(null);
                return;
            }

            if (JsonManager.Instance == null)
            {
                Debug.LogError("[EquipmentDataLoader] JsonManager 未初始化，无法解析装备表。");
                EquipmentCatalog.ReplaceAll(System.Array.Empty<ItemConfigDefinition>());
                CraftRecipeCatalog.RebuildFrom(null);
                ShopCatalog.RebuildFrom(null);
                return;
            }

            var result = JsonManager.Instance.DeserializeFromFilePath<EquipmentDataFileDto>(full, JsonSerializerProfile.GameContent);
            if (!result.Success)
            {
                Debug.LogError($"[EquipmentDataLoader] 解析失败: {result.Error}");
                EquipmentCatalog.ReplaceAll(System.Array.Empty<ItemConfigDefinition>());
                CraftRecipeCatalog.RebuildFrom(null);
                ShopCatalog.RebuildFrom(null);
                return;
            }

            var dto = result.Value;
            EquipmentCatalog.ReplaceAll(dto.Items ?? System.Array.Empty<ItemConfigDefinition>());
            ShopCatalog.RebuildFrom(dto);
            CraftRecipeCatalog.RebuildFrom(dto?.CraftRecipes);
            EquipmentCatalog.ValidateLoadedData(_validateBuffDataAfterLoad, _validateBuffRegistryAfterLoad);
            Debug.Log($"[EquipmentDataLoader] 已加载装备 {dto.Items?.Count ?? 0} 条 (schema {dto.SchemaVersion})");
        }
    }
}
