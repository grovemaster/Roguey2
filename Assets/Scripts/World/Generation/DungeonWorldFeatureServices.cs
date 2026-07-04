using JRogue.Hazards;
using JRogue.Interactables;
using JRogue.Manager.Door;
using JRogue.Traps;
using UnityEngine;

namespace JRogue.World.Generation
{
    /// <summary>
    /// Ensures singleton world-feature services exist on dungeon scene systems roots.
    /// </summary>
    public static class DungeonWorldFeatureServices
    {
        public static void EnsureOn(GameObject systemsRoot)
        {
            if (systemsRoot == null)
                return;

            if (systemsRoot.GetComponent<HazardService>() == null)
                systemsRoot.AddComponent<HazardService>();

            if (systemsRoot.GetComponent<TrapService>() == null)
                systemsRoot.AddComponent<TrapService>();

            if (systemsRoot.GetComponent<InteractableTileService>() == null)
                systemsRoot.AddComponent<InteractableTileService>();

            if (systemsRoot.GetComponent<DoorService>() == null)
                systemsRoot.AddComponent<DoorService>();

            if (systemsRoot.GetComponent<PartyFloorPresenceService>() == null)
                systemsRoot.AddComponent<PartyFloorPresenceService>();
        }
    }
}
