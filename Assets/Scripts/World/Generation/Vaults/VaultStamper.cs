using JRogue.Data.Door;
using JRogue.Hazards;
using JRogue.Interactables;
using JRogue.Item;
using JRogue.Manager.Door;
using JRogue.Spawn;
using JRogue.Manager.Floor;
using JRogue.Manager.Map;
using JRogue.World.Generation;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace JRogue.World.Generation.Vaults
{
    internal static class VaultStamper
    {
        public static bool TryStamp(
            VaultBlueprint blueprint,
            VaultAssetRegistry registry,
            Vector3Int placementOrigin,
            DungeonGenerationContext context,
            out string error)
        {
            error = null;
            if (blueprint == null || registry == null || context == null)
            {
                error = "Missing vault stamp inputs.";
                return false;
            }

            MapManager map = MapManager.Instance;
            if (map == null)
            {
                error = "MapManager missing.";
                return false;
            }

            if (!blueprint.TryGetDefaultFloorTileKey(out string defaultFloorKey))
            {
                error = "Vault has no floor tile keys defined.";
                return false;
            }

            if (!registry.TryResolveTile(defaultFloorKey, out TileBase _))
            {
                error = $"Unknown default floor tile key '{defaultFloorKey}'.";
                return false;
            }

            DoorService doors = DoorService.Instance;
            EnsureDoorOverlay(context, doors, blueprint.VaultId, placementOrigin);
            int mapDoorGlyphs = 0;

            foreach (VaultMapCell cell in blueprint.OccupiedCells())
            {
                Vector3Int world = blueprint.LocalToWorld(placementOrigin, cell.X, cell.Y);
                VaultTileGlyph glyph = cell.Glyph;

                switch (glyph.Kind)
                {
                    case VaultCellKind.Floor:
                        if (!TryPaintFloor(registry, map, world, glyph.TileKey, out error))
                            return false;
                        break;
                    case VaultCellKind.Wall:
                        if (!TryPaintWall(registry, map, world, glyph.TileKey, out error))
                            return false;
                        break;
                    case VaultCellKind.Door:
                        mapDoorGlyphs++;
                        if (!TryPaintFloor(registry, map, world, glyph.TileKey, out error))
                            return false;

                        Debug.Log(
                            $"[VaultDoor] MAP 'D' vault={blueprint.VaultId} local=({cell.X},{cell.Y}) " +
                            $"world=({world.x},{world.y}) registryId={glyph.DoorRegistryId ?? VaultTileGlyph.DefaultDoorRegistryId} " +
                            $"floorKey={glyph.TileKey}");
                        RegisterMapDoor(registry, doors, glyph, world, blueprint.VaultId, cell.X, cell.Y, unlocked: true);
                        break;
                }
            }

            Debug.Log(
                $"[VaultDoor] Stamp tiles done vault={blueprint.VaultId} origin=({placementOrigin.x},{placementOrigin.y}) " +
                $"mapDoorGlyphs={mapDoorGlyphs} doorService={(doors != null ? "ok" : "null")} " +
                $"registeredCells={(doors != null ? doors.RegisteredCellCount : 0)}");

            if (!registry.TryResolveTile(defaultFloorKey, out TileBase defaultFloorTile))
            {
                error = $"Unknown default floor tile key '{defaultFloorKey}'.";
                return false;
            }

            StampEntities(blueprint, registry, placementOrigin, context, doors, defaultFloorTile);
            if (doors != null)
            {
                doors.RefreshAllOverlays(logContext: $"post-stamp:{blueprint.VaultId}");
                Debug.Log(
                    $"[VaultDoor] Post-stamp refresh vault={blueprint.VaultId} registeredCells={doors.RegisteredCellCount}");
            }

            VaultPlacementUtility.ReserveFootprint(blueprint, placementOrigin, context);
            map.FloorMap?.CompressBounds();
            map.WallMap?.CompressBounds();
            return true;
        }

        static bool TryPaintFloor(
            VaultAssetRegistry registry,
            MapManager map,
            Vector3Int world,
            string tileKey,
            out string error)
        {
            error = null;
            if (!registry.TryResolveTile(tileKey, out TileBase tile) || tile == null)
            {
                error = $"Unknown floor tile key '{tileKey}'.";
                return false;
            }

            map.SetCellFloor(world, tile);
            return true;
        }

        static bool TryPaintWall(
            VaultAssetRegistry registry,
            MapManager map,
            Vector3Int world,
            string tileKey,
            out string error)
        {
            error = null;
            if (!registry.TryResolveTile(tileKey, out TileBase tile) || tile == null)
            {
                error = $"Unknown wall tile key '{tileKey}'.";
                return false;
            }

            map.SetCellWall(world, tile);
            return true;
        }

        static void EnsureDoorOverlay(
            DungeonGenerationContext context,
            DoorService doors,
            string vaultId,
            Vector3Int placementOrigin)
        {
            if (doors == null)
            {
                Debug.LogWarning($"[VaultDoor] EnsureDoorOverlay vault={vaultId}: DoorService.Instance is null.");
                return;
            }

            Tilemap overlay = context.Instance?.Tilemaps?.DoorOverlayMap;
            if (overlay != null)
            {
                doors.SetOverlayMap(overlay, logContext: $"vault-stamp:{vaultId}");
                Debug.Log(
                    $"[VaultDoor] Bound overlay vault={vaultId} tilemap={overlay.name} origin=({placementOrigin.x},{placementOrigin.y})");
            }
            else
            {
                Debug.LogWarning(
                    $"[VaultDoor] No DoorOverlayMap on floor instance vault={vaultId} " +
                    $"instance={(context.Instance != null ? context.Instance.name : "null")}");
            }
        }

        static void StampEntities(
            VaultBlueprint blueprint,
            VaultAssetRegistry registry,
            Vector3Int placementOrigin,
            DungeonGenerationContext context,
            DoorService doors,
            TileBase defaultFloorTile)
        {
            DungeonFloorTilemaps tilemaps = context.Instance?.Tilemaps;
            FloorItemPileService piles = FloorItemPileService.Instance;
            HazardService hazards = HazardService.Instance;
            InteractableTileService interactables = InteractableTileService.Instance;
            doors ??= DoorService.Instance;

            if (hazards != null && tilemaps != null)
                hazards.SetOverlayMap(tilemaps.HazardOverlayMap);

            if (interactables != null && tilemaps != null)
                interactables.SetOverlayMap(tilemaps.InteractableOverlayMap);

            if (doors != null && tilemaps != null)
                doors.SetOverlayMap(tilemaps.DoorOverlayMap);

            for (int i = 0; i < blueprint.Items.Count; i++)
            {
                VaultItemPlacement placement = blueprint.Items[i];
                if (!registry.TryResolveItem(placement.ItemId, out ItemData item) || item == null)
                {
                    DungeonGenerationLog.Warn($"Vault '{blueprint.VaultId}': unknown item '{placement.ItemId}'.");
                    continue;
                }

                if (piles == null)
                    continue;

                Vector3Int world = blueprint.LocalToWorld(placementOrigin, placement.X, placement.Y);
                piles.AddEntry(world, new ItemInstance(item, qty: placement.Quantity));
            }

            for (int i = 0; i < blueprint.Interactables.Count; i++)
            {
                VaultInteractablePlacement placement = blueprint.Interactables[i];
                if (!registry.TryResolveInteractable(placement.InteractableId, out InteractableTileDefinition definition)
                    || definition == null)
                {
                    DungeonGenerationLog.Warn(
                        $"Vault '{blueprint.VaultId}': unknown interactable '{placement.InteractableId}'.");
                    continue;
                }

                if (interactables == null)
                    continue;

                Vector3Int world = blueprint.LocalToWorld(placementOrigin, placement.X, placement.Y);
                interactables.Register(world, definition);
            }

            for (int i = 0; i < blueprint.Hazards.Count; i++)
            {
                VaultHazardPlacement placement = blueprint.Hazards[i];
                if (!registry.TryResolveHazard(placement.HazardId, out EnvironmentalHazardDefinition definition)
                    || definition == null)
                {
                    DungeonGenerationLog.Warn($"Vault '{blueprint.VaultId}': unknown hazard '{placement.HazardId}'.");
                    continue;
                }

                if (hazards == null)
                    continue;

                Vector3Int world = blueprint.LocalToWorld(placementOrigin, placement.X, placement.Y);
                hazards.Register(world, definition, startHidden: false);
            }

            for (int i = 0; i < blueprint.Doors.Count; i++)
            {
                VaultDoorPlacement placement = blueprint.Doors[i];
                if (!registry.TryResolveDoor(placement.DoorId, out DoorDefinition definition) || definition == null)
                {
                    DungeonGenerationLog.Warn($"Vault '{blueprint.VaultId}': unknown door '{placement.DoorId}'.");
                    continue;
                }

                if (doors == null)
                    continue;

                Vector3Int world = blueprint.LocalToWorld(placementOrigin, placement.X, placement.Y);
                if (defaultFloorTile != null)
                    MapManager.Instance?.SetCellFloor(world, defaultFloorTile);

                Debug.Log(
                    $"[VaultDoor] DOOR line vault={blueprint.VaultId} local=({placement.X},{placement.Y}) " +
                    $"world=({world.x},{world.y}) id={placement.DoorId}");
                RegisterDoor(
                    doors,
                    definition,
                    world,
                    blueprint.VaultId,
                    placement.X,
                    placement.Y,
                    placement.Unlocked,
                    startOpen: false);
            }

            Transform enemyParent = context.Instance?.EnemyContainer;
            for (int i = 0; i < blueprint.Enemies.Count; i++)
            {
                VaultEnemyPlacement placement = blueprint.Enemies[i];
                if (!registry.TryResolveEnemy(placement.EnemyId, out EnemySpawnDefinition definition)
                    || definition == null)
                {
                    DungeonGenerationLog.Warn(
                        $"Vault '{blueprint.VaultId}': unknown enemy '{placement.EnemyId}'.");
                    continue;
                }

                Vector3Int world = blueprint.LocalToWorld(placementOrigin, placement.X, placement.Y);
                if (!EnemySpawnService.TrySpawnAtExactCell(definition, world, out _, enemyParent))
                {
                    DungeonGenerationLog.Warn(
                        $"Vault '{blueprint.VaultId}': could not spawn '{placement.EnemyId}' at ({placement.X},{placement.Y}).");
                }
            }
        }

        static void RegisterMapDoor(
            VaultAssetRegistry registry,
            DoorService doors,
            VaultTileGlyph glyph,
            Vector3Int world,
            string vaultId,
            int localX,
            int localY,
            bool unlocked)
        {
            if (doors == null)
            {
                Debug.LogWarning($"[VaultDoor] RegisterMapDoor skipped vault={vaultId} local=({localX},{localY}): DoorService null.");
                return;
            }

            if (doors.TryGetAtCell(world, out DoorInstance existing))
            {
                Debug.Log(
                    $"[VaultDoor] RegisterMapDoor skipped vault={vaultId} world=({world.x},{world.y}): " +
                    $"already has door '{existing.DoorId}' state={existing.State}.");
                return;
            }

            string doorId = string.IsNullOrEmpty(glyph.DoorRegistryId)
                ? VaultTileGlyph.DefaultDoorRegistryId
                : glyph.DoorRegistryId;

            if (!registry.TryResolveDoor(doorId, out DoorDefinition definition) || definition == null)
            {
                DungeonGenerationLog.Warn($"MAP door glyph references unknown door id '{doorId}'.");
                Debug.LogWarning(
                    $"[VaultDoor] Registry miss vault={vaultId} local=({localX},{localY}) world=({world.x},{world.y}) id='{doorId}'.");
                return;
            }

            Debug.Log(
                $"[VaultDoor] Resolved vault={vaultId} id='{doorId}' -> def={definition.name} " +
                $"doorId={definition.doorId} orient={definition.orientation} startsOpen={definition.startsOpen}");
            RegisterDoor(doors, definition, world, vaultId, localX, localY, unlocked, startOpen: false);
        }

        static void RegisterDoor(
            DoorService doors,
            DoorDefinition definition,
            Vector3Int world,
            string vaultId,
            int localX,
            int localY,
            bool unlocked,
            bool startOpen)
        {
            if (doors == null || definition == null)
            {
                Debug.LogWarning(
                    $"[VaultDoor] RegisterDoor aborted vault={vaultId} local=({localX},{localY}): " +
                    $"doors={(doors != null)} def={(definition != null)}.");
                return;
            }

            Debug.Log(
                $"[VaultDoor] Register vault={vaultId} local=({localX},{localY}) world=({world.x},{world.y}) " +
                $"unlocked={unlocked} startOpen={startOpen}");
            doors.Register(new DoorPlacement
            {
                definition = definition,
                cell = world,
                overrideLocked = true,
                startsLocked = !unlocked,
                overrideOpenState = true,
                initialState = startOpen ? DoorState.Open : DoorState.Closed,
            });
        }
    }
}
