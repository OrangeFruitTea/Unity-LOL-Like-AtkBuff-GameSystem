using System.Collections.Generic;
using Core.ECS;

namespace Core.Entity
{
    public struct EntityDataComponent : IEcsComponent
    {
        // 核心数据
        private Dictionary<EntityBaseDataCore, double> _coreData;
        // 额外数据
        private Dictionary<EntityBaseData, double> _bonusData;
        
        public void InitializeDefaults()
        {
            _coreData = new Dictionary<EntityBaseDataCore, double>
            {
                { EntityBaseDataCore.HpLimit, 1000 }, // 默认生命值上限1000
                { EntityBaseDataCore.CrtHp, 1000 }, // 默认当前生命值=上限
                { EntityBaseDataCore.MpLimit, 200 }, // 默认魔法值上限200
                { EntityBaseDataCore.CrtMp, 200 }, // 默认当前魔法值=上限
                { EntityBaseDataCore.AtkAD, 100 }, // 默认物理攻击100
                { EntityBaseDataCore.AtkAP, 50 }, // 默认魔法攻击50
                { EntityBaseDataCore.DefenceAD, 50 }, // 默认物理防御50
                { EntityBaseDataCore.DefenceAP, 30 }, // 默认魔法防御30
                { EntityBaseDataCore.AtkSpeed, 1.0 }, // 默认攻击速度1.0
                { EntityBaseDataCore.SkillCd, 1.0 }, // 默认技能冷却倍率1.0（无冷却缩减）
                { EntityBaseDataCore.CriticalRate, 0.05 }, // 默认暴击率5%
                { EntityBaseDataCore.MoveSpeed, 3.5 } // 默认移动速度3.5
            };
            _bonusData = new Dictionary<EntityBaseData, double>
            {
                { EntityBaseData.HpRecoverPerSecond, 5 }, // 默认每秒回血5
                { EntityBaseData.MpRecoverPerSecond, 2 }, // 默认每秒回蓝2
                { EntityBaseData.PenAD, 0 }, // 默认物理穿透0
                { EntityBaseData.PenAP, 0 }, // 默认魔法穿透0
                { EntityBaseData.LifeSteal, 0 }, // 默认生命偷取0
                { EntityBaseData.OmniVamp, 0 }, // 默认全能吸血0
                { EntityBaseData.AtkDistance, 1.5 }, // 默认攻击距离1.5
                { EntityBaseData.CriticalDamage, 1.5 }, // 默认暴击伤害150%
                { EntityBaseData.Resilience, 0 } // 默认韧性0
            };
        }

        public double GetData(EntityBaseDataCore data)
        {
            if (_coreData == null) return 0;
            return _coreData.GetValueOrDefault(data);
        }

        public double GetData(EntityBaseData data)
        {
            if (_bonusData == null) return 0;
            return _bonusData.GetValueOrDefault(data);
        }

        public IReadOnlyDictionary<EntityBaseData, double> GetBonusData()
        {
            return _bonusData ?? (IReadOnlyDictionary<EntityBaseData, double>) new Dictionary<EntityBaseData, double>();
        }

        public IReadOnlyDictionary<EntityBaseDataCore, double> GetCoreData()
        {
            return _coreData ?? (IReadOnlyDictionary<EntityBaseDataCore, double>) new Dictionary<EntityBaseDataCore, double>();
        }

        public void SetData(EntityBaseDataCore data, double value)
        {
            _coreData ??= new Dictionary<EntityBaseDataCore, double>();
            _coreData[data] = value;
        }
        
        public void SetData(EntityBaseData data, double value)
        {
            _bonusData ??= new Dictionary<EntityBaseData, double>();
            _bonusData[data] = value;
        }
    }
}
