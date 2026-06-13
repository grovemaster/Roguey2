using JRogue.Actors;
using JRogue.World.Generation;

namespace JRogue.Racial
{
    /// <summary>
    /// Safe-zone gate for Dragonian spell memorize / unmemorize loadout edits.
    /// </summary>
    public static class DragonianSpellLoadoutService
    {
        public static bool TryMemorize(BaseActor actor, string spellId, out string failureReason)
        {
            failureReason = null;
            if (actor == null)
            {
                failureReason = "No actor.";
                return false;
            }

            if (!SafeZonePolicyService.TryAllowDragonianMemorizeChange(out failureReason))
                return false;

            DragonianSpellsRuntime runtime = actor.GetComponent<DragonianSpellsRuntime>();
            if (runtime == null)
            {
                failureReason = "No Dragonian spell runtime.";
                return false;
            }

            return runtime.TryMemorize(spellId, out failureReason);
        }

        public static bool TryUnmemorize(BaseActor actor, string spellId, out string failureReason)
        {
            failureReason = null;
            if (actor == null)
            {
                failureReason = "No actor.";
                return false;
            }

            if (!SafeZonePolicyService.TryAllowDragonianMemorizeChange(out failureReason))
                return false;

            DragonianSpellsRuntime runtime = actor.GetComponent<DragonianSpellsRuntime>();
            if (runtime == null)
            {
                failureReason = "No Dragonian spell runtime.";
                return false;
            }

            if (!runtime.TryUnmemorize(spellId))
            {
                failureReason = $"Spell '{spellId}' is not memorized.";
                return false;
            }

            return true;
        }
    }
}
