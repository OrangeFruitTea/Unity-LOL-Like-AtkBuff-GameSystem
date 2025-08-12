using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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
            // 读取json文件
            string filePath = Path.Combine(Application.streamingAssetsPath, "BuffData.json");
            string jsonContent = File.ReadAllText(filePath);
            // 反序列化
            BuffJsonDataWrapper wrapper = JsonUtility.FromJson<BuffJsonDataWrapper>($"{{\"Buffs\": {jsonContent}}}");
            foreach (var data in wrapper.buffs)
            {
                if (!_buffDataMap.TryAdd(data.config.id, data))
                    Debug.LogWarning($"buffId重复：{data.config.id}");
            }
            Debug.Log($"已加载{_buffDataMap.Count}个buff配置");
        }
        catch (Exception e) {
            Debug.LogError($"加载buff数据出错：{e.Message}");
        }
    }
}