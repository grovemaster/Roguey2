using JRogue.Actors;
using JRogue.Manager.Essence;
using JRogue.Manager.Equipment;
using JRogue.Stats.Racial;
using UnityEngine;

namespace JRogue.Racial
{
    public static class HumanPriestVowLogic
    {
        public static bool MeetsCompletionGates(PriestVowDefinition vow, out string failureReason)
        {
            failureReason = null;
            if (vow == null)
            {
                failureReason = "Unknown vow.";
                return false;
            }

            // v0: floor/time gates stub — satisfied when reporting at shrine after dungeon return.
            // Full integration hooks dungeon floor index + day/night counters later.
            return true;
        }

        public static bool IsVowBroken(PriestVowDefinition vow, GameObject priestActor, string triggerId)
        {
            if (vow == null || priestActor == null)
                return false;

            switch (vow.ruleKind)
            {
                case PriestVowRuleKind.NoBladedWeapons:
                    return triggerId == "bladed_weapon";
                case PriestVowRuleKind.InvokeOnlyAtFullHealth:
                    return triggerId == "invoke_not_full_hp";
                case PriestVowRuleKind.NoCarryEssenceItems:
                    return triggerId == "carry_essence";
                default:
                    return false;
            }
        }

        public static bool IsPartyVowBroken(PriestVowDefinition vow, BaseActor actor, string triggerId)
        {
            if (vow == null || actor == null)
                return false;

            if (vow.ruleKind == PriestVowRuleKind.PartyNoEssenceConsumption
                && triggerId == "essence_equipped")
            {
                return true;
            }

            return false;
        }

        public static void NotifyEssenceEquipped(BaseActor actor)
        {
            if (actor == null)
                return;

            HumanPriestVowService.NotifyPartyAction(actor, "essence_equipped");

            EssenceSlotManager essence = actor.GetComponent<EssenceSlotManager>();
            if (essence == null)
                return;

            // Priest personal essence vow check is N/A — priests cannot equip essences.
        }
    }
}
