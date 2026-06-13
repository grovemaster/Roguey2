using System.Collections.Generic;
using System.Text;
using JRogue.Actors;
using JRogue.Item;
using JRogue.Stats;
using UnityEngine;

namespace JRogue.Progression.Proficiency
{
    public readonly struct ProficiencyAward
    {
        public ProficiencyAward(ProficiencyKind kind, int pxp)
        {
            Kind = kind;
            Pxp = pxp;
        }

        public ProficiencyKind Kind { get; }
        public int Pxp { get; }
    }

    public static class ProficiencyXpDispatcher
    {
        public static IReadOnlyList<ProficiencyAward> BuildAwards(ProficiencyResolvedAction action)
        {
            if (action == null)
                return System.Array.Empty<ProficiencyAward>();

            int basePxp = ResolveBasePxp(action);
            var awards = new Dictionary<ProficiencyKind, int>();

            if (action.HasWeaponType)
            {
                ProficiencyKind weaponKind = ProficiencyKindMapping.FromWeaponType(action.WeaponType);
                AddFull(awards, weaponKind, basePxp);
            }

            if (action.DamageModulesApplied != null)
            {
                float damageFraction = action.SpellDamageTypesAtHalfRate
                    ? ProficiencyRules.SpellDamageTypeFraction
                    : 1f;

                for (int i = 0; i < action.DamageModulesApplied.Count; i++)
                {
                    DamageEntry module = action.DamageModulesApplied[i];
                    if (module.value <= 0)
                        continue;

                    ProficiencyKind damageKind = ProficiencyKindMapping.FromDamageType(module.type);
                    AddFull(awards, damageKind, ScalePxp(basePxp, damageFraction));
                }
            }

            if (action.ProficiencyTags != null)
            {
                for (int i = 0; i < action.ProficiencyTags.Count; i++)
                    AddFull(awards, action.ProficiencyTags[i], basePxp);
            }

            if (action.CountsAsWeaponHit)
                AddFull(awards, ProficiencyKind.Fighting, ScalePxp(basePxp, ProficiencyRules.FightingSecondaryFraction));

            return FlattenAwards(awards);
        }

        public static void Dispatch(BaseActor actor, ProficiencyResolvedAction action)
        {
            if (actor == null || action == null)
                return;

            CharacterStats stats = actor.stats;
            ProficiencyRuntime runtime = ProficiencyRuntime.EnsureOn(actor.gameObject);
            if (stats == null || runtime == null)
                return;

            IReadOnlyList<ProficiencyAward> awards = BuildAwards(action);
            if (awards.Count == 0)
                return;

            var logParts = new StringBuilder();
            logParts.Append("[Proficiency] ").Append(actor.DisplayName).Append(':');

            for (int i = 0; i < awards.Count; i++)
            {
                ProficiencyAward award = awards[i];
                if (!ProficiencyEligibility.CanTrain(stats, award.Kind))
                    continue;

                runtime.AddPxp(stats, award.Kind, award.Pxp);
                logParts.Append(' ').Append('+').Append(award.Pxp).Append(' ').Append(award.Kind);
            }

            Debug.Log(logParts.ToString());
        }

        static int ResolveBasePxp(ProficiencyResolvedAction action)
        {
            if (action.ProficiencyXpOverride > 0)
                return action.ProficiencyXpOverride;

            return action.Tier switch
            {
                ProficiencyActionTier.HeavyHit => 18,
                ProficiencyActionTier.SpellCast => 15,
                ProficiencyActionTier.CheapCantrip => 8,
                ProficiencyActionTier.ArmourTick => 6,
                ProficiencyActionTier.TrapDodge => 10,
                _ => 12,
            };
        }

        static void AddFull(Dictionary<ProficiencyKind, int> awards, ProficiencyKind kind, int pxp)
        {
            if (kind == ProficiencyKind.None || pxp <= 0)
                return;

            if (awards.TryGetValue(kind, out int existing))
                awards[kind] = existing + pxp;
            else
                awards[kind] = pxp;
        }

        static int ScalePxp(int basePxp, float fraction) =>
            Mathf.Max(1, Mathf.RoundToInt(basePxp * fraction));

        static IReadOnlyList<ProficiencyAward> FlattenAwards(Dictionary<ProficiencyKind, int> awards)
        {
            if (awards.Count == 0)
                return System.Array.Empty<ProficiencyAward>();

            var list = new List<ProficiencyAward>(awards.Count);
            foreach (KeyValuePair<ProficiencyKind, int> pair in awards)
                list.Add(new ProficiencyAward(pair.Key, pair.Value));

            if (list.Count > ProficiencyRules.MaxAwardsPerAction)
                list.RemoveRange(ProficiencyRules.MaxAwardsPerAction, list.Count - ProficiencyRules.MaxAwardsPerAction);

            return list;
        }
    }
}
