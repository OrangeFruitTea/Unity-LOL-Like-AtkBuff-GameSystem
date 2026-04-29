using System;
using System.Collections.Generic;

namespace Gameplay.Skill.Buff
{
    /// <summary>
    /// buff 配置 id → <see cref="IBuffFactory"/>；表驱动施加前必须注册对应具体 Buff 类型。
    /// </summary>
    public static class BuffTypeRegistry
    {
        private static readonly Dictionary<int, IBuffFactory> Factories = new Dictionary<int, IBuffFactory>();

        public static void Clear()
        {
            Factories.Clear();
        }

        public static void Register<TBuff>(int buffConfigId) where TBuff : BuffBase, new()
        {
            Factories[buffConfigId] = new TypedBuffFactory<TBuff>();
        }

        public static void RegisterFactory(int buffConfigId, IBuffFactory factory)
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));
            Factories[buffConfigId] = factory;
        }

        public static bool TryGetFactory(int buffConfigId, out IBuffFactory factory) =>
            Factories.TryGetValue(buffConfigId, out factory);
    }
}
