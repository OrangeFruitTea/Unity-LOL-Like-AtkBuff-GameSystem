using System;
using System.Collections.Generic;
using System.Linq;

namespace Basement.Json.Migration
{
    /// <summary>
    /// 按版本链注册迁移器，将 JSON 从 <paramref name="currentVersion"/> 升到 <paramref name="targetVersion"/>。
    /// </summary>
    public sealed class JsonConfigMigrationRegistry
    {
        private readonly List<IJsonConfigMigrator> _migrators = new List<IJsonConfigMigrator>();

        public void Register(IJsonConfigMigrator migrator)
        {
            if (migrator == null)
                throw new ArgumentNullException(nameof(migrator));
            _migrators.Add(migrator);
        }

        public global::Basement.Json.JsonReadResult<string> Migrate(string json, int currentVersion, int targetVersion)
        {
            if (json == null)
                return global::Basement.Json.JsonReadResult<string>.Fail("json is null");
            if (currentVersion > targetVersion)
                return global::Basement.Json.JsonReadResult<string>.Fail("cannot downgrade schema");
            if (currentVersion == targetVersion)
                return global::Basement.Json.JsonReadResult<string>.Ok(json);

            string working = json;
            int v = currentVersion;
            while (v < targetVersion)
            {
                var step = _migrators
                    .Where(m => m.FromVersion == v && m.ToVersion > v)
                    .OrderBy(m => m.ToVersion)
                    .FirstOrDefault();

                if (step == null)
                    return global::Basement.Json.JsonReadResult<string>.Fail($"no migrator registered from version {v}");

                try
                {
                    working = step.Migrate(working);
                }
                catch (Exception ex)
                {
                    return global::Basement.Json.JsonReadResult<string>.Fail($"migrate {v}->{step.ToVersion}: {ex.Message}");
                }

                v = step.ToVersion;
            }

            return global::Basement.Json.JsonReadResult<string>.Ok(working);
        }
    }
}
