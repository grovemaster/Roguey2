using System.Collections.Generic;
using JRogue.Manager.Map;
using JRogue.World.Generation.Phases;
using JRogue.World.Generation.Vaults;
using JRogue.World.Lighting;
using UnityEngine;

namespace JRogue.World.Generation.Zones
{
    /// <summary>
    /// Places glow-floor emitters only in walkable cells that remain under-lit after wall emitters register.
    /// </summary>
    public static class ZoneGlowFloorGapFillApplicator
    {
        const string DefaultTorchResourcePath = "Lighting/Torch";
        const int GlowFloorLayerSalt = 2;

        public static int Apply(
            DungeonGenerationContext context,
            MapManager map,
            LightingService lighting)
        {
            if (context == null || !context.UsesZoneComposite || map == null || lighting == null)
                return 0;

            DungeonFloorZoneLayout layout = context.Definition?.ZoneLayout;
            if (layout?.ZoneDefinitions == null)
                return 0;

            if (!PopulationPlacementUtility.TryGetMapBounds(context, out int width, out int height))
                return 0;

            LightEmitterDefinition defaultEmitter = Resources.Load<LightEmitterDefinition>(DefaultTorchResourcePath);
            var paintContext = ZoneTilePaintContext.From(context);
            var placed = new List<Vector3Int>();
            int applied = 0;
            int skippedReserved = 0;

            DungeonZoneDefinition[] definitions = layout.ZoneDefinitions;
            for (int z = 0; z < definitions.Length; z++)
            {
                DungeonZoneDefinition zoneDef = definitions[z];
                if (zoneDef == null)
                    continue;

                ZoneFillProfile profile = zoneDef.FillProfile;
                if (!profile.glowFloorGapFill || profile.glowFloorPalette == null)
                    continue;

                int minReceived = Mathf.Max(0, profile.glowFloorMinReceivedLight);
                int minSpacing = Mathf.Max(1, profile.glowFloorMinSpacing);
                string zoneId = zoneDef.ZoneId;

                var candidates = new List<(Vector3Int cell, int receivedLight, int sortHash)>();
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        Vector3Int cell = new Vector3Int(x, y, 0);
                        if (!map.IsWalkable(cell))
                            continue;

                        if (context.ReservedCells.Contains(cell))
                        {
                            skippedReserved++;
                            continue;
                        }

                        if (!context.TryGetZoneId(cell, out string cellZoneId) || cellZoneId != zoneId)
                            continue;

                        int received = lighting.GetReceivedLight(cell);
                        if (!ZoneGlowFloorGapFillLogic.NeedsGlowFill(received, minReceived))
                            continue;

                        int sortHash = DungeonTilePaletteResolver.ComputeCellHash(
                            paintContext,
                            zoneId,
                            cell,
                            GlowFloorLayerSalt);
                        candidates.Add((cell, received, sortHash));
                    }
                }

                candidates.Sort((a, b) =>
                {
                    int byLight = a.receivedLight.CompareTo(b.receivedLight);
                    if (byLight != 0)
                        return byLight;

                    return a.sortHash.CompareTo(b.sortHash);
                });

                for (int i = 0; i < candidates.Count; i++)
                {
                    Vector3Int cell = candidates[i].cell;
                    if (ZoneGlowFloorGapFillLogic.IsWithinSpacing(cell, placed, minSpacing))
                        continue;

                    if (!profile.glowFloorPalette.TryPickEntry(
                            cell,
                            zoneId,
                            paintContext,
                            GlowFloorLayerSalt,
                            out DungeonTilePaletteEntry entry)
                        || entry.tile == null
                        || !entry.isLightEmitter)
                    {
                        continue;
                    }

                    map.SetCellFloor(cell, entry.tile);
                    if (RegisterGlowEmitter(cell, entry, defaultEmitter, lighting, zoneId))
                    {
                        placed.Add(cell);
                        applied++;
                    }
                }
            }

            if (skippedReserved > 0)
            {
                Debug.Log(
                    $"{VaultStampDiagnostics.Tag} GlowGapFill skippedReserved={skippedReserved} " +
                    $"(vault footprint cells not overwritten)");
            }

            return applied;
        }

        static bool RegisterGlowEmitter(
            Vector3Int cell,
            DungeonTilePaletteEntry entry,
            LightEmitterDefinition defaultEmitter,
            LightingService lighting,
            string zoneId)
        {
            LightEmitterDefinition definition = entry.emitLight != null ? entry.emitLight : defaultEmitter;
            if (definition == null)
                return false;

            int emission = entry.emissionOverride > 0
                ? entry.emissionOverride
                : definition.BaseEmissionMax;

            lighting.EnableEmitter(cell, definition, emission, "glow floor gap fill", zoneId);
            return true;
        }
    }
}
