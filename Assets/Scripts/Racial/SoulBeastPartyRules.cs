using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Manager.Party;
using JRogue.Stats;
using JRogue.Stats.Racial;

namespace JRogue.Racial
{
    public static class SoulBeastPartyRules
    {
        public static List<BaseActor> GetEligibleBeastmen(bool requireUnbonded)
        {
            var beastmen = new List<BaseActor>();
            PartyManager party = PartyManager.Instance;
            if (party == null)
                return beastmen;

            foreach (BaseActor member in party.partyMembers)
            {
                if (member == null || !member.gameObject.activeInHierarchy)
                    continue;

                if (!IsEligibleBeastman(member, requireUnbonded, out _))
                    continue;

                beastmen.Add(member);
            }

            return beastmen;
        }

        public static bool IsEligibleBeastman(BaseActor actor, bool requireUnbonded, out string rejectReason)
        {
            rejectReason = null;
            if (actor == null)
            {
                rejectReason = "No Beastman selected.";
                return false;
            }

            CharacterStats stats = actor.stats;
            if (stats == null || stats.race != Race.Beastman)
            {
                rejectReason = "Target is not a Beastman.";
                return false;
            }

            if (stats.racialSubsystem != RacialSubsystemKind.BeastmanSoulBeast)
            {
                rejectReason = "This Beastman cannot perform Soul Beast rituals.";
                return false;
            }

            BeastmanSoulBeastRuntime runtime = actor.GetComponent<BeastmanSoulBeastRuntime>();
            if (runtime == null)
            {
                rejectReason = "No Soul Beast runtime.";
                return false;
            }

            if (requireUnbonded && runtime.IsBonded)
            {
                rejectReason = $"{actor.DisplayName} is already bound to a Soul Beast.";
                return false;
            }

            if (!requireUnbonded && !runtime.IsBonded)
            {
                rejectReason = $"{actor.DisplayName} has no Soul Beast contract.";
                return false;
            }

            return true;
        }

        public static BaseActor FindBondedBeastmanForBloodUse(out string rejectReason)
        {
            rejectReason = null;
            List<BaseActor> bonded = GetEligibleBeastmen(requireUnbonded: false);
            if (bonded.Count == 0)
            {
                rejectReason = "Requires a Beastman bonded to a Soul Beast.";
                return null;
            }

            return bonded[0];
        }

        public static bool CanUseBeastBlood(out string rejectReason)
        {
            BaseActor target = FindBondedBeastmanForBloodUse(out rejectReason);
            if (target == null)
                return false;

            BeastmanSoulBeastRuntime runtime = target.GetComponent<BeastmanSoulBeastRuntime>();
            if (runtime == null || !runtime.TryResolveBondedDefinition(out SoulBeastDefinition beast))
            {
                rejectReason = "Unknown Soul Beast contract.";
                return false;
            }

            int cap = SoulBeastProgressionLogic.GetEffectiveLevelCap(target.stats, beast);
            if (runtime.SoulBeastLevel >= cap)
            {
                rejectReason =
                    $"Soul Beast level cannot exceed {target.DisplayName}'s level ({cap}).";
                return false;
            }

            rejectReason = null;
            return true;
        }
    }
}
