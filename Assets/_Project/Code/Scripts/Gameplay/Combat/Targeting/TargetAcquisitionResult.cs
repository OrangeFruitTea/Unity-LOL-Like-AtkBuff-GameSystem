using System;
using System.Collections.Generic;
using Core.Entity;

namespace Gameplay.Combat.Targeting
{
    public sealed class TargetAcquisitionResult
    {
        private static readonly EntityBase[] EmptyHits = Array.Empty<EntityBase>();

        public bool Succeeded { get; }
        public string Error { get; }
        public IReadOnlyList<EntityBase> Hits { get; }
        public EntityBase SuggestedPrimary { get; }

        private TargetAcquisitionResult(
            bool succeeded,
            string error,
            EntityBase[] hits,
            EntityBase suggestedPrimary)
        {
            Succeeded = succeeded;
            Error = error ?? string.Empty;
            Hits = hits ?? EmptyHits;
            SuggestedPrimary = suggestedPrimary;
        }

        public static TargetAcquisitionResult Fail(string error) =>
            new TargetAcquisitionResult(false, error ?? string.Empty, EmptyHits, null);

        public static TargetAcquisitionResult Ok(EntityBase[] hits, EntityBase suggestedPrimary) =>
            new TargetAcquisitionResult(true, string.Empty, hits, suggestedPrimary);
    }
}
