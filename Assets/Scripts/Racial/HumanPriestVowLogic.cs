using JRogue.Actors;
using JRogue.World.Generation;
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

            DungeonTimeService time = DungeonTimeService.Instance;
            if (time == null || !time.DungeonRunActive)
                return true;

            int floorIndex = HumanPriestVowProgressService.ResolveCurrentFloorIndex();
            if (floorIndex < vow.minFloorIndex)
            {
                failureReason =
                    $"Delve deeper before reporting this vow (floor {floorIndex}/{vow.minFloorIndex}).";
                return false;
            }

            int cycles = HumanPriestVowProgressService.ResolveElapsedDayNightCycles();
            if (cycles < vow.minDayNightInDungeon)
            {
                failureReason =
                    $"Spend more time in the delve before reporting "
                    + $"({cycles}/{vow.minDayNightInDungeon} day-night cycles).";
                return false;
            }

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

        public static bool IsPartyVowBroken(PriestVowDefinition vow, JRogue.Actors.BaseActor actor, string triggerId)
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

        public static void NotifyEssenceEquipped(JRogue.Actors.BaseActor actor)
        {
            if (actor == null)
                return;

            HumanPriestVowService.NotifyPartyAction(actor, "essence_equipped");
        }

        public static void NotifyBladedWeaponEquipped(JRogue.Actors.BaseActor actor)
        {
            if (actor == null)
                return;

            HumanPriestVowService.NotifyPersonalAction(actor.gameObject, "bladed_weapon");
        }

        public static void NotifyInvokeNotAtFullHealth(GameObject priestActor)
        {
            if (priestActor == null)
                return;

            HumanPriestVowService.NotifyPersonalAction(priestActor, "invoke_not_full_hp");
        }
    }
}
