using System;
using System.Collections.Generic;
using System.IO;
using Basement.Logging;
using Basement.Json;
using UnityEngine;

[Serializable]
public class BuffJsonDataWrapper
{
    public List<BuffJsonData> buffs;
}

[Serializable]
public class BuffJsonData
{
    public BuffMetadata metadata;
    public BuffConfig config;
}

public class BuffDataLoader : MonoBehaviour
{
    private Dictionary<int, BuffJsonData> _buffDataMap = new Dictionary<int, BuffJsonData>();
    public static BuffDataLoader Instance { get; private set; }

    /// <summary> 配置是否已加载且包含指定 id（技能管线校验 Buff 表用）。 </summary>
    public bool TryGet(int buffId, out BuffJsonData data) =>
        _buffDataMap.TryGetValue(buffId, out data);

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadBuffJsonData();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void LoadBuffJsonData()
    {
        try
        {
            string filePath = Path.Combine(Application.streamingAssetsPath, "BuffData.json");
            
            if (!File.Exists(filePath))
            {
                LogManager.Instance.LogError($"Buff数据文件不存在: {filePath}", "BuffDataLoader");
                return;
            }

            string jsonContent = File.ReadAllText(filePath);
            BuffJsonDataWrapper wrapper = JsonManager.Instance.Deserialize<BuffJsonDataWrapper>($"{{\"buffs\": {jsonContent}}}");
            
            if (wrapper?.buffs != null)
            {
                foreach (var data in wrapper.buffs)
                {
                    if (!_buffDataMap.TryAdd(data.config.id, data))
                        LogManager.Instance.LogWarning($"buffId重复：{data.config.id}", "BuffDataLoader");
                }
                LogManager.Instance.LogInfo($"已加载{_buffDataMap.Count}个buff配置", "BuffDataLoader");
            }
            else
            {
                LogManager.Instance.LogError("反序列化Buff数据失败", "BuffDataLoader");
            }
        }
        catch (Exception e) {
            LogManager.Instance.LogError($"加载buff数据出错：{e.Message}", "BuffDataLoader");
        }
    }
}