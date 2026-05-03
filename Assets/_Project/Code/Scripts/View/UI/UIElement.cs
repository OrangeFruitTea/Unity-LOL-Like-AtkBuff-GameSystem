using System;
using Core.ECS;
using UnityEngine;

namespace Core.UI
{
    /// <summary>
    /// 需运行时绑定 <see cref="EcsEntityBridge"/> 以读取 ECS 数据的 UI 组件（如 HUD）。
    /// </summary>
    public interface IEntityBridgeBindable
    {
        void Bind(EcsEntityBridge bridge);
    }

    public class UIElement
    {
        public string PrefabPath;
        public Vector2 Position;
        public Vector2 Size;
        public UILayerType LayerType;
        public UIAnchorType AnchorType;
        public bool IsGenerated;
        public Guid UiInstanceId;
        public bool DontDestroyOnLoad;

        public void SetDefaults()
        {
            PrefabPath = string.Empty;
            Position = Vector2.zero;
            Size = Vector2.zero;
            LayerType = UILayerType.MainUI;
            AnchorType = UIAnchorType.Center;
            IsGenerated = false;
            UiInstanceId = Guid.Empty;
            DontDestroyOnLoad = false;
        }
    }
}
