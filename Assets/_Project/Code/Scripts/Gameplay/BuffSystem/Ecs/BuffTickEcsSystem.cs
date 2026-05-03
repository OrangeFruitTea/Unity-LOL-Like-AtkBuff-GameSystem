using Core.ECS;
using UnityEngine;

namespace Gameplay.BuffSystem.Ecs
{
    /// <summary>
    /// 将 <see cref="BuffManager"/> 的周期 Tick / 持续时间衰减 / 字典清扫挂到 <see cref="EcsWorld"/> 更新链，
    /// 与 <see cref="ImpactSystem"/>、<see cref="SkillCastPipelineSystem"/> 同源注册（见 <see cref="Gameplay.Runtime.GameplaySystemsBootstrap"/>）。
    /// </summary>
    public sealed class BuffTickEcsSystem : IEcsSystem
    {
        /// <summary> 夹在 UnitVitality(32) 与 JungleAi(38) 之间。 </summary>
        public int UpdateOrder => 33;

        private float _periodicAccum;

        private float _gcAccum;

        public void Initialize()
        {
            _periodicAccum = 0f;
            _gcAccum = 0f;
            _ = BuffManager.Instance;
        }

        public void Destroy()
        {
        }

        public void Update()
        {
            var bm = BuffManager.Instance;
            if (bm == null)
                return;

            float dt = Time.deltaTime;
            bm.PumpDurationDecay(dt);

            _periodicAccum += dt;
            while (_periodicAccum >= BuffManager.FixedDeltaTime)
            {
                _periodicAccum -= BuffManager.FixedDeltaTime;
                bm.PumpBuffPeriodicSteps();
            }

            _gcAccum += dt;
            if (_gcAccum >= 10f)
            {
                _gcAccum = 0f;
                bm.PumpGarbageCollection();
            }
        }
    }
}
