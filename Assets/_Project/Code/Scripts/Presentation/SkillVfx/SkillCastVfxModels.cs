using System;
using UnityEngine;

namespace Gameplay.Presentation.SkillVfx
{
    /// <summary>与 <see cref="SkillExecutionFacade.TryBeginCast"/> 成功对齐的播放时机。</summary>
    public enum SkillFxMoment
    {
        OnCastSucceeded = 0,
        AfterDelaySeconds = 1
    }

    /// <summary>特效原点语义（位置由本枚举 + <see cref="SkillVfxSpawnSpec.localPositionOffset"/> 组合）。</summary>
    public enum SkillFxAttachKind
    {
        CasterRoot = 0,
        CasterHandRight = 1,
        CasterHandLeft = 2,
        GroundUnderCaster = 3,
        PrimaryTargetRoot = 4,
        GroundUnderPrimaryTarget = 5
    }

    /// <summary>技能 → Prefab 的一条映射；在 Inspector 中配置。</summary>
    [Serializable]
    public struct SkillVfxSpawnSpec
    {
        [Tooltip("与 SkillCatalog / SkillData.json 中 skillId 一致")]
        public int skillId;

        public GameObject fxPrefab;

        public SkillFxMoment moment;

        [Tooltip("仅当 moment = AfterDelaySeconds 时使用")]
        public float delaySeconds;

        public SkillFxAttachKind attach;

        public Vector3 localPositionOffset;

        public Vector3 localEulerAngles;

        [Tooltip("true：附加节点的旋转 × localEuler；false：仅使用 localEuler 为世界旋转")]
        public bool multiplyAttachRotation;

        [Tooltip("true：设为锚点子物体（跟随骨骼）；false：挂在场景根下")]
        public bool parentToAttachTransform;
    }
}
