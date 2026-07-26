using JRogue.Interactables;
using JRogue.World.Altar;
using JRogue.World.MapInteract;
using UnityEngine;

namespace JRogue.World.Generation.Vaults
{
    /// <summary>
    /// Converts Northern Dark <c>vault_altar_3x3</c> bump flavor into an offering altar for rift portals.
    /// </summary>
    public static class RiftPedestalPlacementLogic
    {
        public const string VaultId = "vault_altar_3x3";

        public static bool TryGetPedestalCell(VaultBlueprint blueprint, Vector3Int origin, out Vector3Int cell)
        {
            cell = default;
            if (blueprint?.Interactables == null || blueprint.Interactables.Count == 0)
                return false;

            VaultInteractablePlacement interactable = blueprint.Interactables[0];
            cell = blueprint.LocalToWorld(origin, interactable.X, interactable.Y);
            return true;
        }

        public static void OnPlaced(DungeonGenerationContext context, VaultBlueprint blueprint, Vector3Int origin)
        {
            if (context == null || blueprint == null || blueprint.VaultId != VaultId)
                return;

            if (!TryGetPedestalCell(blueprint, origin, out Vector3Int cell))
                return;

            AltarDefinition altarDef = context.Definition?.RiftPolicy?.riftPedestalAltar;
            if (altarDef == null)
            {
                Debug.LogWarning(
                    "[Rift] vault_altar_3x3 placed but floor rift policy has no riftPedestalAltar — leaving bump interactable.");
                return;
            }

            InteractableTileService.Instance?.UnregisterAtCell(cell);

            AdjacentMapInteractableService mapInteract = AdjacentMapInteractableService.Instance;
            if (mapInteract == null)
            {
                Debug.LogWarning("[Rift] AdjacentMapInteractableService missing — cannot register rift pedestal altar.");
                return;
            }

            if (context.Instance != null)
                mapInteract.SetOverlayMap(context.Instance.InteractableOverlayMap);

            AltarBootstrap.Register(mapInteract, cell, altarDef);
            if (mapInteract.TryGetAtCell(cell, out IAdjacentMapInteractable interactable) && interactable != null)
                context.Instance?.RegisterMapInteractable(interactable);

            Debug.Log($"[Rift] Pedestal altar registered at {cell}.");
        }
    }
}
