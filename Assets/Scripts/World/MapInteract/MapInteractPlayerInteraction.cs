using System.Collections.Generic;
using JRogue.Actors;
using JRogue.UI.Gameplay;
using UnityEngine;

namespace JRogue.World.MapInteract
{
    public static class MapInteractPlayerInteraction
    {
        public const string LogPrefix = "[MapInteract]";

        public static bool TryInteractAdjacent(BaseActor actor)
        {
            if (actor == null)
                return false;

            AdjacentMapInteractableService service = AdjacentMapInteractableService.Instance;
            if (service == null)
            {
                Debug.LogWarning($"{LogPrefix} No {nameof(AdjacentMapInteractableService)} in scene.");
                return false;
            }

            IReadOnlyList<IAdjacentMapInteractable> candidates = service.GetInteractableCandidates(actor);
            if (candidates.Count == 0)
            {
                Debug.Log($"{LogPrefix} Nothing to interact with nearby.");
                return false;
            }

            if (candidates.Count == 1)
            {
                candidates[0].OpenInteractUI(actor);
                return true;
            }

            AdjacentInteractPickerModalUI.EnsureInstance().Show(
                actor,
                candidates,
                selected => selected?.OpenInteractUI(actor));

            return true;
        }
    }
}
