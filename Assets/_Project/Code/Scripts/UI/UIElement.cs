using System;
using UnityEngine;

namespace Core.UI
{
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
