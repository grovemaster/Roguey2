using UnityEngine;

namespace JRogue.Racial
{
    /// <summary>
    /// Invokes racial loadout passive hooks without requiring <see cref="JRogue.Manager.Essence.EssenceSlotManager"/>.
    /// </summary>
    public static class RacialPassiveHooks
    {
        public static void RefreshPassives(GameObject actor) =>
            actor.GetComponent<RacialLoadoutApplier>()?.RefreshPassives();

        public static void NotifyTurnStart(GameObject actor) =>
            actor.GetComponent<RacialLoadoutApplier>()?.NotifyPassivesTurnStart();
    }
}
