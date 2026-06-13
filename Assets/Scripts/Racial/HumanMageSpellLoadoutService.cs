using JRogue.Actors;
using JRogue.World.Generation;

namespace JRogue.Racial
{
    /// <summary>
    /// Safe-zone gate for Human Mage spell equip / unequip loadout edits.
    /// </summary>
    public static class HumanMageSpellLoadoutService
    {
        public static bool TryEquip(BaseActor actor, string spellId, out string failureReason)
        {
            failureReason = null;
            if (actor == null)
            {
                failureReason = "No actor.";
                return false;
            }

            if (!SafeZonePolicyService.TryAllowHumanMageEquipChange(out failureReason))
                return false;

            HumanMageSpellsRuntime runtime = actor.GetComponent<HumanMageSpellsRuntime>();
            if (runtime == null)
            {
                failureReason = "No Human Mage spell runtime.";
                return false;
            }

            return runtime.TryEquip(spellId, out failureReason);
        }

        public static bool TryUnequip(BaseActor actor, string spellId, out string failureReason)
        {
            failureReason = null;
            if (actor == null)
            {
                failureReason = "No actor.";
                return false;
            }

            if (!SafeZonePolicyService.TryAllowHumanMageEquipChange(out failureReason))
                return false;

            HumanMageSpellsRuntime runtime = actor.GetComponent<HumanMageSpellsRuntime>();
            if (runtime == null)
            {
                failureReason = "No Human Mage spell runtime.";
                return false;
            }

            if (!runtime.TryUnequip(spellId))
            {
                failureReason = $"Spell '{spellId}' is not equipped.";
                return false;
            }

            return true;
        }
    }
}
