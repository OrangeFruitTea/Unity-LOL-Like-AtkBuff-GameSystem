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

        public ImpactModifier(string type, float value, bool isPercentage, string source)
        {
            Type = type;
            Value = value;
            IsPercentage = isPercentage;
            Source = source;
            Timestamp = Time.time;
        }
    }
}
