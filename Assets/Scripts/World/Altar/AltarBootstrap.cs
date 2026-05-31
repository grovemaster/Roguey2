using System;
using JRogue.World.MapInteract;
using UnityEngine;

namespace JRogue.World.Altar
{
    public sealed class AltarBootstrap : MonoBehaviour
    {
        [SerializeField] AltarPlacementSet placementSet;
        [SerializeField] AltarPlacement[] placements = Array.Empty<AltarPlacement>();

        void Start()
        {
            AdjacentMapInteractableService service = AdjacentMapInteractableService.Instance;
            if (service == null)
                return;

            AltarPlacement[] source = placementSet != null && placementSet.placements != null
                && placementSet.placements.Length > 0
                ? placementSet.placements
                : placements;

            for (int i = 0; i < source.Length; i++)
            {
                AltarPlacement placement = source[i];
                if (placement.definition == null)
                    continue;

                Register(service, placement.cell, placement.definition);
            }
        }

        public static void Register(
            AdjacentMapInteractableService service,
            Vector3Int cell,
            AltarDefinition definition)
        {
            if (service == null || definition == null)
                return;

            var instance = new AltarInstance(cell, definition);
            var interactable = new AltarInteractable(instance);
            service.Register(cell, interactable);

            if (definition.overlaySprite != null)
                service.PaintOverlay(cell, definition.overlaySprite);
        }
    }
}
