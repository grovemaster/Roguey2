using JRogue.Manager.Map;
using JRogue.World.Generation.Phases;
using JRogue.World.Lighting;
using UnityEngine;

namespace JRogue.World.Generation.Zones
{
    public static class ZoneTileEmitterApplicator
    {
        const string DefaultTorchResourcePath = "Lighting/Torch";

        public static int Apply(
            DungeonGenerationContext context,
            MapManager map,
            LightingService lighting)
        {
            if (context == null || !context.UsesZoneComposite || map == null || lighting == null)
                return 0;

            DungeonFloorZoneLayout layout = context.Definition?.ZoneLayout;
            if (layout == null)
                return 0;

            if (!PopulationPlacementUtility.TryGetMapBounds(context, out int width, out int height))
                return 0;

            LightEmitterDefinition defaultEmitter = Resources.Load<LightEmitterDefinition>(DefaultTorchResourcePath);
            var paintContext = ZoneTilePaintContext.From(context);
            int applied = 0;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Vector3Int cell = new Vector3Int(x, y, 0);
                    if (!context.TryGetZoneId(cell, out string zoneId) || string.IsNullOrEmpty(zoneId))
                        continue;

                    if (map.IsWalkable(cell))
                    {
                        if (!DungeonTilePaletteResolver.UsesGlowFloorGapFill(context.Definition?.ZoneLayout, zoneId)
                            && TryRegisterFloorEmitter(
                                map,
                                layout,
                                context.Definition,
                                zoneId,
                                cell,
                                paintContext,
                                defaultEmitter,
                                lighting))
                        {
                            applied++;
                        }
                    }
                    else if (TryRegisterWallEmitter(
                        map,
                        layout,
                        context.Definition,
                        zoneId,
                        cell,
                        paintContext,
                        defaultEmitter,
                        lighting))
                    {
                        applied++;
                    }
                }
            }

            if (applied > 0)
                lighting.FinalizeRegistry();

            return applied;
        }

        static bool TryRegisterFloorEmitter(
            MapManager map,
            DungeonFloorZoneLayout layout,
            DungeonFloorDefinition floorDef,
            string zoneId,
            Vector3Int cell,
            ZoneTilePaintContext paintContext,
            LightEmitterDefinition defaultEmitter,
            LightingService lighting)
        {
            DungeonTilePalette palette = DungeonTilePaletteResolver.ResolveFloorPalette(layout, floorDef, zoneId);
            if (palette == null || !palette.TryPickEntry(
                    cell,
                    zoneId,
                    paintContext,
                    DungeonTilePaletteResolver.FloorLayerSalt,
                    out DungeonTilePaletteEntry entry))
            {
                return false;
            }

            if (!entry.isLightEmitter)
                return false;

            return RegisterEmitter(cell, entry, defaultEmitter, lighting, zoneId);
        }

        static bool TryRegisterWallEmitter(
            MapManager map,
            DungeonFloorZoneLayout layout,
            DungeonFloorDefinition floorDef,
            string zoneId,
            Vector3Int cell,
            ZoneTilePaintContext paintContext,
            LightEmitterDefinition defaultEmitter,
            LightingService lighting)
        {
            DungeonTilePalette palette = DungeonTilePaletteResolver.ResolveWallPalette(layout, floorDef, zoneId);
            if (palette == null || !palette.TryPickEntry(
                    cell,
                    zoneId,
                    paintContext,
                    DungeonTilePaletteResolver.WallLayerSalt,
                    out DungeonTilePaletteEntry entry))
            {
                return false;
            }

            if (!entry.isLightEmitter)
                return false;

            return RegisterEmitter(cell, entry, defaultEmitter, lighting, zoneId);
        }

        static bool RegisterEmitter(
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

            lighting.RegisterPending(
                cell,
                LightCellData.Emitter(definition, emission, zoneId: zoneId),
                overwrite: true);
            return true;
        }
    }
}
