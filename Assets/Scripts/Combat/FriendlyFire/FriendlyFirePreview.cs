using System.Collections.Generic;
using JRogue.Ability;
using JRogue.Actors;
using JRogue.Core.Actor;
using JRogue.Core.Targeting;
using JRogue.Input;
using JRogue.Manager.Party;
using UnityEngine;

namespace JRogue.Combat.FriendlyFire
{
    public static class FriendlyFirePreview
    {
        public struct Result
        {
            public bool WouldHarmAllies;
            public IReadOnlyList<BaseActor> AffectedAllies;
            public string ActionLabel;
        }

        static readonly Result Empty = new Result
        {
            WouldHarmAllies = false,
            AffectedAllies = System.Array.Empty<BaseActor>(),
            ActionLabel = string.Empty,
        };

        public static Result Evaluate(
            BaseActor caster,
            in TargetedActionContext context,
            Vector3Int primaryTile)
        {
            if (caster == null)
                return Empty;

            string actionLabel = TargetedActionResolver.ResolveActionLabel(caster, context);

            if (context.Source == PlayerAbilitySource.BowAim)
                return EvaluateBow(caster, primaryTile, actionLabel);

            AbilityAction ability = TargetedActionResolver.ResolveAbility(caster, context);
            if (ability == null || ability.skipFriendlyFireConfirmation)
                return Empty;

            var splashContext = new SplashZoneContext(
                caster.GridPosition,
                primaryTile,
                caster.currentFacing);
            IReadOnlyList<Vector3Int> cells = SplashZoneResolver.GetEffectCells(
                ability.ResolveSplashZone(),
                splashContext);
            List<IBattleTarget> targets = TargetingResolver.GetTargetsInCells(cells);
            List<BaseActor> allies = CollectAffectedAllies(caster, targets, ability, caster.gameObject);

            return new Result
            {
                WouldHarmAllies = allies.Count > 0,
                AffectedAllies = allies,
                ActionLabel = actionLabel,
            };
        }

        static Result EvaluateBow(BaseActor caster, Vector3Int primaryTile, string actionLabel)
        {
            if (!BowRangedCombatService.WouldHarmPartyAlly(caster, primaryTile, out List<BaseActor> allies))
                return Empty;

            return new Result
            {
                WouldHarmAllies = true,
                AffectedAllies = allies,
                ActionLabel = actionLabel,
            };
        }

        public static List<BaseActor> CollectAffectedAllies(
            BaseActor caster,
            IReadOnlyList<IBattleTarget> targets,
            AbilityAction ability,
            GameObject casterObject)
        {
            var harmed = new HashSet<BaseActor>();
            PartyManager party = PartyManager.Instance;

            if (targets == null || ability == null || party?.partyMembers == null)
                return new List<BaseActor>();

            for (int i = 0; i < targets.Count; i++)
            {
                if (targets[i] is not BaseActor actor)
                    continue;

                if (!IsLivingPartyAlly(caster, actor, party))
                    continue;

                if (!ability.WouldHarm(targets[i], casterObject))
                    continue;

                harmed.Add(actor);
            }

            return OrderByPartyRoster(harmed, party);
        }

        public static bool IsLivingPartyAlly(BaseActor caster, BaseActor actor, PartyManager party)
        {
            if (caster == null || actor == null || party?.partyMembers == null)
                return false;

            if (actor == caster)
                return false;

            if (!party.partyMembers.Contains(actor))
                return false;

            if (!actor.gameObject.activeInHierarchy)
                return false;

            return actor.stats == null || actor.stats.currentHP > 0;
        }

        public static List<BaseActor> OrderByPartyRoster(ISet<BaseActor> harmed, PartyManager party)
        {
            var ordered = new List<BaseActor>();
            if (harmed == null || harmed.Count == 0 || party?.partyMembers == null)
                return ordered;

            for (int i = 0; i < party.partyMembers.Count; i++)
            {
                BaseActor member = party.partyMembers[i];
                if (member != null && harmed.Contains(member))
                    ordered.Add(member);
            }

            return ordered;
        }
    }
}
