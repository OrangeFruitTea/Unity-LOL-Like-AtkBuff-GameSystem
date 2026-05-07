using UnityEngine;

namespace Gameplay.Presentation.Interaction
{
    /// <summary>
    /// 与设计文档 §3.1 对齐的 Rim 材质属性名；Shader Graph / HLSL 须使用相同 Property 名。
    /// </summary>
    public static class RimPresentationShaderIds
    {
        public static readonly int RimColor = Shader.PropertyToID("_RimColor");
        public static readonly int RimStrength = Shader.PropertyToID("_RimStrength");
        public static readonly int RimPower = Shader.PropertyToID("_RimPower");
    }
}
