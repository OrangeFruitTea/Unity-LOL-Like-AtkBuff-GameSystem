using Core.ECS;

namespace Core.Entity
{
    /// <summary>
    /// 战斗期轻量黑板：攻击/仇恨/承伤/击杀/助攻等 **逻辑实体 id**（0=无效）。<br/>
    /// **主攻目标** 与 <c>TowerCombatCycle</c>、Impact 对齐时使用 <see cref="AttackTargetEntityId"/>；详见《MOBA局内单位模块ECS设计文档》§8。<br/>
    /// 不向组件内写入 UnityEngine.Object。
    /// </summary>
    public struct CombatBoardLiteComponent : IEcsComponent
    {
        /// <summary>
        /// 当前普攻或技能 **单体** 主攻/瞄准对象（与射线、Strike 对齐）。<br/>
        /// NPC/塔/野怪索敌后写入；玩家从范围/选中确定目标后也应写入，供与 <c>SkillCastContext.PrimaryTarget</c> 对齐。多目标/AoE 落点另用施法上下文。
        /// </summary>
        public long AttackTargetEntityId;

        /// <summary> 仇恨首要对象（简易单槽；可与 <see cref="AttackTargetEntityId"/> 相同，或嘲讽、塔仇恨等规则下分离）。 </summary>
        public long ThreatTargetEntityId;

        /// <summary> 最近一次对自身造成伤害的进攻方实体。 </summary>
        public long LastDamageFromEntityId;

        /// <summary> 被击倒时记载的击杀者；存活中为 0。由 <c>UnitVitalitySystem</c>（或等价）写入。 </summary>
        public long KillerEntityId;

        /// <summary> 助攻候选人（毕设至多 3 槽，经济结算按需填写）。无效为 0。 </summary>
        public long AssistEntityId0;

        public long AssistEntityId1;

        public long AssistEntityId2;

        public void InitializeDefaults()
        {
            AttackTargetEntityId = 0;
            ThreatTargetEntityId = 0;
            LastDamageFromEntityId = 0;
            KillerEntityId = 0;
            AssistEntityId0 = 0;
            AssistEntityId1 = 0;
            AssistEntityId2 = 0;
        }
    }
}
