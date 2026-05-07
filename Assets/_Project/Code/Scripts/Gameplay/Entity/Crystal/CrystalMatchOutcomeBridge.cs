using System.Threading;
using Basement.Events;
using Core.ECS;
using Core.Entity;
using UnityEngine;

namespace Gameplay.Entity
{
    /// <summary>
    /// 监听 <see cref="CombatUnitDiedGameEvent"/>：若阵亡者带 <see cref="CrystalCoreObjectiveComponent"/>，则一次性裁定胜负并派发 <see cref="CrystalCoreDestroyedMatchEndGameEvent"/>。<br/>
    /// 场景放一个实例即可；不参与 ECS Update。参见设计文档 §6～§7。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CrystalMatchOutcomeBridge : MonoBehaviour
    {
        [Header("两方阵营 Id（单机 MOBA Demo；与 OwningTeamId / Faction 对齐）")]
        [SerializeField]
        private byte teamBlueId = (byte)FactionTeamId.Blue;

        [SerializeField]
        private byte teamRedId = (byte)FactionTeamId.Red;

        [Header("降级行为（尚无 MatchFlow 时）")]
        [SerializeField]
        private bool pauseTimeScaleWhenCrystalDestroyed = true;

        [SerializeField]
        private bool publishCrystalDestroyedEndEvent = true;

        [SerializeField]
        private bool logOutcomeToConsole = true;

        private GameEventHandlerDelegate<CombatUnitDiedGameEvent> _deathHandler;

        private int _endLatch;

        private void Awake()
        {
            _deathHandler = OnCombatUnitDiedEvent;
        }

        private void OnEnable()
        {
            var bus = GameEventBus.Instance;
            if (bus == null)
            {
                Debug.LogWarning($"[{nameof(CrystalMatchOutcomeBridge)}] GameEventBus missing; crystal match end inactive.");
                return;
            }

            bus.Initialize();
            bus.Subscribe(_deathHandler);
        }

        private void OnDisable()
        {
            GameEventBus.Instance?.Unsubscribe(_deathHandler);
        }

        /// <summary>新对局重置门闩；由场景加载/MatchFlow 在开局调用。</summary>
        public void ResetMatchEndLatch()
        {
            Interlocked.Exchange(ref _endLatch, 0);
        }

        private void OnCombatUnitDiedEvent(CombatUnitDiedGameEvent ev)
        {
            if (ev == null || EcsWorld.Instance == null)
                return;

            var victimId = ev.VictimEntityId;
            if (victimId == 0L)
                return;

            var victim = new EcsEntity(victimId);
            if (!victim.IsValid())
                return;

            if (!victim.HasComponent<CrystalCoreObjectiveComponent>())
                return;

            var crystalCore = victim.GetComponent<CrystalCoreObjectiveComponent>();

            if (Interlocked.CompareExchange(ref _endLatch, 1, 0) != 0)
                return;

            var losingTeam = crystalCore.OwningTeamId;
            if (!TryResolveWinnerTeam(losingTeam, out var winningTeam))
            {
                Debug.LogWarning(
                    $"[{nameof(CrystalMatchOutcomeBridge)}] Crystal destroyed but OwningTeamId={losingTeam} not in [{teamBlueId},{teamRedId}]; latch released.");
                Interlocked.Exchange(ref _endLatch, 0);
                return;
            }

            if (logOutcomeToConsole)
            {
                Debug.Log(
                    $"[{nameof(CrystalMatchOutcomeBridge)}] Crystal match end — losingTeam={losingTeam} winningTeam={winningTeam} " +
                    $"crystalEcsId={victimId} killerEcsId={ev.KillerEntityId}");
            }

            if (pauseTimeScaleWhenCrystalDestroyed && Time.timeScale > 1e-5f)
                Time.timeScale = 0f;

            if (!publishCrystalDestroyedEndEvent)
                return;

            GameEventBus.Instance.Initialize();
            GameEventBus.Instance.Publish(new CrystalCoreDestroyedMatchEndGameEvent
            {
                LosingTeamId = losingTeam,
                WinningTeamId = winningTeam,
                CrystalEntityId = victimId,
                KillerEntityId = ev.KillerEntityId
            });
        }

        private bool TryResolveWinnerTeam(byte losingTeamId, out byte winningTeamId)
        {
            if (losingTeamId == teamBlueId && teamBlueId != teamRedId)
            {
                winningTeamId = teamRedId;
                return true;
            }

            if (losingTeamId == teamRedId && teamBlueId != teamRedId)
            {
                winningTeamId = teamBlueId;
                return true;
            }

            winningTeamId = 0;
            return false;
        }
    }
}
