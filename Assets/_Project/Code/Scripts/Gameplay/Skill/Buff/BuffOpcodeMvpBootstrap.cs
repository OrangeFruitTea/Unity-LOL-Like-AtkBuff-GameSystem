using UnityEngine;

namespace Gameplay.Skill.Buff
{
    /// <summary>
    /// 注册 MVP opcode Buff（<see cref="BuffOpcodeMvpDefinitions"/>）。与 <see cref="Gameplay.Runtime.GameplaySystemsBootstrap"/> 同为 AfterSceneLoad。
    /// </summary>
    public static class BuffOpcodeMvpBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void RegisterMvpOpcodeBuffs()
        {
            BuffTypeRegistry.RegisterFactory(
                BuffOpcodeMvpDefinitions.InstantMagicDamageTest,
                new MetaBuffApplyFactory(BuffOpcodeMvpDefinitions.InstantMagicDamageTest));
            BuffTypeRegistry.RegisterFactory(
                BuffOpcodeMvpDefinitions.SimplePeriodicMagicDotTest,
                new MetaBuffApplyFactory(BuffOpcodeMvpDefinitions.SimplePeriodicMagicDotTest));

            Debug.Log("[BuffOpcodeMvpBootstrap] MetaBuff factories 90001 / 90002 registered.");
        }
    }
}
