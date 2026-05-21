using JRogue.Stats;
using UnityEngine;

namespace JRogue.Racial
{
    /// <summary>
    /// Apply/remove/refresh for <see cref="IRacialProgressionPayload"/> (benefits, restrictions, stats, passives).
    /// </summary>
    public static class RacialProgressionPayloadApplicator
    {
        public static void Apply(GameObject target, CharacterStats stats, object source, IRacialProgressionPayload payload)
        {
            if (payload == null || source == null)
                return;

            if (payload.RacialRestrictions != null && target != null)
            {
                foreach (RacialRestrictionDefinition restriction in payload.RacialRestrictions)
                    restriction?.OnApply(target);
            }

            if (payload.RacialBenefits != null && target != null)
            {
                foreach (RacialBenefitDefinition benefit in payload.RacialBenefits)
                    benefit?.OnApply(target);
            }

            RacialAbilityPayloadApplicator.Apply(
                target,
                stats,
                source,
                payload.StatModifiers,
                payload.ResistanceModifiers,
                payload.PassiveEffects);
        }

        public static void Remove(GameObject target, CharacterStats stats, object source, IRacialProgressionPayload payload)
        {
            if (payload == null || source == null)
                return;

            RacialAbilityPayloadApplicator.Remove(
                target,
                stats,
                source,
                payload.StatModifiers,
                payload.ResistanceModifiers,
                payload.PassiveEffects);

            if (payload.RacialBenefits != null && target != null)
            {
                for (int i = payload.RacialBenefits.Count - 1; i >= 0; i--)
                    payload.RacialBenefits[i]?.OnRemove(target);
            }

            if (payload.RacialRestrictions != null && target != null)
            {
                for (int i = payload.RacialRestrictions.Count - 1; i >= 0; i--)
                    payload.RacialRestrictions[i]?.OnRemove(target);
            }
        }

        public static void RefreshPassives(GameObject target, IRacialProgressionPayload payload)
        {
            if (payload == null)
                return;

            if (payload.RacialBenefits != null && target != null)
            {
                foreach (RacialBenefitDefinition benefit in payload.RacialBenefits)
                    benefit?.Refresh(target);
            }

            RacialAbilityPayloadApplicator.RefreshPassives(target, payload.PassiveEffects);
        }

        public static void NotifyPassivesTurnStart(GameObject target, IRacialProgressionPayload payload)
        {
            if (payload == null)
                return;

            if (payload.RacialBenefits != null && target != null)
            {
                foreach (RacialBenefitDefinition benefit in payload.RacialBenefits)
                    benefit?.OnTurnStart(target);
            }

            RacialAbilityPayloadApplicator.NotifyPassivesTurnStart(target, payload.PassiveEffects);
        }
    }
}
