using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Manager.Essence;
using JRogue.Manager.Party;
using JRogue.Stats.Racial;
using JRogue.World.Generation;
using UnityEngine;

namespace JRogue.Racial
{
    public static class HumanPriestDevotionLoadoutService
    {
        public static bool TryAllowEdit(BaseActor actor, out string failureReason)
        {
            failureReason = null;
            if (actor == null)
            {
                failureReason = "No speaker.";
                return false;
            }

            return SafeZonePolicyService.TryAllowHumanPriestDevotionChange(out failureReason);
        }

        public static bool TryEquip(
            BaseActor actor,
            string invocationId,
            out string failureReason)
        {
            failureReason = null;
            if (!SafeZonePolicyService.TryAllowHumanPriestDevotionChange(out failureReason))
                return false;

            HumanPriestDevotionRuntime devotion = actor?.GetComponent<HumanPriestDevotionRuntime>();
            if (devotion == null)
            {
                failureReason = "No priest devotion runtime.";
                return false;
            }

            return devotion.TryEquip(invocationId, out failureReason);
        }

        public static bool TryUnequip(BaseActor actor, string invocationId, out string failureReason)
        {
            failureReason = null;
            if (!SafeZonePolicyService.TryAllowHumanPriestDevotionChange(out failureReason))
                return false;

            HumanPriestDevotionRuntime devotion = actor?.GetComponent<HumanPriestDevotionRuntime>();
            if (devotion == null)
            {
                failureReason = "No priest devotion runtime.";
                return false;
            }

            if (!devotion.TryUnequip(invocationId))
            {
                failureReason = "Invocation is not prepared.";
                return false;
            }

            return true;
        }
    }
}
