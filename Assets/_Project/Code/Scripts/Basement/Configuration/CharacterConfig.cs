using System;
using System.Collections.Generic;

namespace Basement.Configuration
{
    /// <summary>
    /// 角色配置
    /// </summary>
    public class CharacterConfig : IConfiguration
    {
        public string Version { get; set; } = "1.0.0";
        public string Name { get; set; }
        public string FilePath { get; set; }
        public DateTime LastModified { get; set; }

        public string CharacterId { get; set; }
        public string DisplayName { get; set; }

        // 核心数据
        public int HpLimit { get; set; } = 1000; // 生命值上限
        public int CrtHp { get; set; } = 1000; // 当前生命值
        public int MpLimit { get; set; } = 200; // 魔法值上限
        public int CrtMp { get; set; } = 200; // 当前魔法值
        public int AtkAD { get; set; } = 100; // 物理攻击
        public int AtkAP { get; set; } = 50; // 魔法攻击
        public int DefenceAD { get; set; } = 50; // 物理防御
        public int DefenceAP { get; set; } = 30; // 魔法防御
        public float AtkSpeed { get; set; } = 1.0f; // 攻击速度
        public float SkillCd { get; set; } = 1.0f; // 技能冷却倍率
        public float CriticalRate { get; set; } = 0.05f; // 暴击率
        public float MoveSpeed { get; set; } = 3.5f; // 移动速度

        // 额外数据
        public float HpRecoverPerSecond { get; set; } = 5.0f; // 每秒回血
        public float MpRecoverPerSecond { get; set; } = 2.0f; // 每秒回蓝
        public float PenAD { get; set; } = 0.0f; // 物理穿透
        public float PenAP { get; set; } = 0.0f; // 魔法穿透
        public float LifeSteal { get; set; } = 0.0f; // 生命偷取
        public float OmniVamp { get; set; } = 0.0f; // 全能吸血
        public float AtkDistance { get; set; } = 1.5f; // 攻击距离
        public float CriticalDamage { get; set; } = 1.5f; // 暴击伤害
        public float Resilience { get; set; } = 0.0f; // 韧性

        public List<string> Skills { get; set; } = new List<string>();
        public Dictionary<string, int> Stats { get; set; } = new Dictionary<string, int>();

        public bool Validate()
        {
            return GetValidationErrors().Count == 0;
        }

        public List<string> GetValidationErrors()
        {
            var errors = new List<string>();

            // 验证角色ID
            if (string.IsNullOrEmpty(CharacterId))
            {
                errors.Add("角色ID不能为空");
            }

            // 验证角色名称
            if (string.IsNullOrEmpty(DisplayName))
            {
                errors.Add("角色名称不能为空");
            }

            // 核心数据验证
            if (HpLimit < 1 || HpLimit > int.MaxValue)
            {
                errors.Add("生命值上限必须大于0");
            }

            if (CrtHp < 0 || CrtHp > HpLimit)
            {
                errors.Add("当前生命值必须在0到生命值上限之间");
            }

            if (MpLimit < 0 || MpLimit > int.MaxValue)
            {
                errors.Add("魔法值上限必须大于等于0");
            }

            if (CrtMp < 0 || CrtMp > MpLimit)
            {
                errors.Add("当前魔法值必须在0到魔法值上限之间");
            }

            if (AtkAD < 0 || AtkAD > int.MaxValue)
            {
                errors.Add("物理攻击必须大于等于0");
            }

            if (AtkAP < 0 || AtkAP > int.MaxValue)
            {
                errors.Add("魔法攻击必须大于等于0");
            }

            if (DefenceAD < 0 || DefenceAD > int.MaxValue)
            {
                errors.Add("物理防御必须大于等于0");
            }

            if (DefenceAP < 0 || DefenceAP > int.MaxValue)
            {
                errors.Add("魔法防御必须大于等于0");
            }

            if (AtkSpeed < 0.1f || AtkSpeed > 10.0f)
            {
                errors.Add("攻击速度必须在0.1-10.0之间");
            }

            if (SkillCd < 0.1f || SkillCd > 10.0f)
            {
                errors.Add("技能冷却倍率必须在0.1-10.0之间");
            }

            if (CriticalRate < 0f || CriticalRate > 1.0f)
            {
                errors.Add("暴击率必须在0-1.0之间");
            }

            if (MoveSpeed < 0.1f || MoveSpeed > 10.0f)
            {
                errors.Add("移动速度必须在0.1-10.0之间");
            }

            // 额外数据验证
            if (HpRecoverPerSecond < 0f || HpRecoverPerSecond > 100.0f)
            {
                errors.Add("每秒回血必须在0-100.0之间");
            }

            if (MpRecoverPerSecond < 0f || MpRecoverPerSecond > 100.0f)
            {
                errors.Add("每秒回蓝必须在0-100.0之间");
            }

            if (PenAD < 0f || PenAD > 100.0f)
            {
                errors.Add("物理穿透必须在0-100.0之间");
            }

            if (PenAP < 0f || PenAP > 100.0f)
            {
                errors.Add("魔法穿透必须在0-100.0之间");
            }

            if (LifeSteal < 0f || LifeSteal > 1.0f)
            {
                errors.Add("生命偷取必须在0-1.0之间");
            }

            if (OmniVamp < 0f || OmniVamp > 1.0f)
            {
                errors.Add("全能吸血必须在0-1.0之间");
            }

            if (AtkDistance < 0.1f || AtkDistance > 20.0f)
            {
                errors.Add("攻击距离必须在0.1-20.0之间");
            }

            if (CriticalDamage < 1.0f || CriticalDamage > 5.0f)
            {
                errors.Add("暴击伤害必须在1.0-5.0之间");
            }

            if (Resilience < 0f || Resilience > 1.0f)
            {
                errors.Add("韧性必须在0-1.0之间");
            }

            return errors;
        }

        /// <summary>
        /// Unity编辑器验证方法
        /// </summary>
        public void OnValidate()
        {
            var errors = GetValidationErrors();
            foreach (var error in errors)
            {
                UnityEngine.Debug.LogError($"配置验证失败: {error}");
            }
        }
    }
}