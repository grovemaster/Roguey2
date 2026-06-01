using JRogue.Actors;
using JRogue.World.MapInteract;
using UnityEngine;

namespace JRogue.World.Generation
{
    /// <summary>
    /// Town plaza portal — step-on shows enter-dungeon dialog; does not use floor-to-floor transition.
    /// </summary>
    public sealed class TownToDungeonPortalInteractable : IAdjacentMapInteractable
    {
        public Vector3Int Cell { get; }
        public string ListLabel { get; }
        public int SortOrder => 5;

        public TownToDungeonPortalInteractable(Vector3Int cell, string listLabel = "Dungeon portal")
        {
            Cell = cell;
            ListLabel = listLabel;
        }

        public bool CanInteract(BaseActor actor) => false;

        public void OpenInteractUI(BaseActor actor) { }

        public bool TryActivate(BaseActor triggeringMember)
        {
            if (triggeringMember == null || DungeonEntryService.EntryScheduled)
                return false;

            DungeonEntryService.RequestEnterDungeonFromTown();
            return false;
        }
    }
}
