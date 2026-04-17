using UnityEngine;

namespace Core.Combat
{
    public struct ImpactModifier
    {
        public string Type; // 修改类型
        public float Value;
        public string Source;
        public bool IsPercentage;
        public float Timestamp;
        public ModifierPriority Priority;

        public ImpactModifier(string type, float value, bool isPercentage, string source, ModifierPriority priority = ModifierPriority.Medium)
        {
            Type = type;
            Value = value;
            IsPercentage = isPercentage;
            Source = source;
            Timestamp = Time.time;
            Priority = priority;
        }
    }
}

