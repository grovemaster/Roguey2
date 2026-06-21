using JRogue.Manager.Map;
using JRogue.World.Generation.Phases;
using JRogue.World.Lighting;
using UnityEngine;

namespace JRogue.World.Generation.Zones
{
    public static class ZoneAmbientApplicator
    {
        public static int Apply(
            DungeonGenerationContext context,
            MapManager map,
            LightingService lighting)
        {
            if (context == null || !context.UsesZoneComposite || context.ZoneCellMap == null
                || map == null || lighting == null)
            {
                return 0;
            }

            DungeonFloorZoneLayout layout = context.Definition?.ZoneLayout;
            if (layout == null)
                return 0;

            if (!PopulationPlacementUtility.TryGetMapBounds(context, out int width, out int height))
                return 0;

            int applied = 0;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Vector3Int cell = new Vector3Int(x, y, 0);
                    if (!HasTerrainTile(map, cell))
                        continue;

                    if (!context.TryGetZoneId(cell, out string zoneId) || string.IsNullOrEmpty(zoneId))
                        continue;

                    if (!layout.TryGetZoneDefinition(zoneId, out DungeonZoneDefinition zoneDef))
                        continue;

                    if (zoneDef.AmbientRegionId < 0)
                        continue;

                    lighting.RegisterPending(
                        cell,
                        LightCellData.Receiver(zoneDef.AmbientRegionId, zoneDef.DefaultAmbientLight, zoneId),
                        overwrite: true);
                    applied++;
                }
            }

            if (applied > 0)
                lighting.FinalizeRegistry();

            return applied;
        }

        static bool HasTerrainTile(MapManager map, Vector3Int cell) =>
            map != null && (map.IsWalkable(cell) || map.IsWall(cell));
    }
}
