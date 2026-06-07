using System.Collections.Generic;
using System.Text;
using JRogue.Manager.Map;
using JRogue.World.Generation.Phases;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace JRogue.World.Generation.Zones
{
    /// <summary>
    /// Verbose generation checkpoints tagged [DungeonGen][ZoneDiag] for Floor 1 zone debugging.
    /// </summary>
    public static class ZoneGenerationDiagnostics
    {
        const string Tag = "[DungeonGen][ZoneDiag]";

        public static void LogCheckpoint(DungeonGenerationContext context, string label)
        {
            if (context == null)
            {
                Debug.LogWarning($"{Tag} {label}: context=null");
                return;
            }

            MapManager map = MapManager.Instance;
            if (map == null)
            {
                Debug.LogWarning($"{Tag} {label}: MapManager=null");
                return;
            }

            if (!PopulationPlacementUtility.TryGetMapBounds(context, out int width, out int height))
            {
                Debug.LogWarning($"{Tag} {label}: invalid map bounds");
                return;
            }

            var log = new StringBuilder();
            log.Append(label);
            log.Append($" floorId={context.Definition?.FloorId}");
            log.Append($" playerStart={context.PlayerStart}");
            log.Append($" layout={context.Definition?.LayoutMode}");
            log.Append($" paintedZone={context.UsesPaintedZoneMap}");
            log.Append($" map={width}x{height}");

            CountWalkableByZone(map, context, width, height, out Dictionary<string, int> walkableByZone, out int walkableTotal);
            log.Append($" walkableTotal={walkableTotal}");
            log.Append(' ').Append(ZoneCellMapStats.FormatCounts(walkableByZone));

            int candidates = PopulationPlacementUtility.CollectFloorCandidates(map, context).Count;
            log.Append($" populationCandidates={candidates}");
            log.Append($" safeZone={context.SafeZoneCells.Count} reserved={context.ReservedCells.Count}");

            AppendReachability(context, map, width, height, walkableTotal, log);
            Debug.Log($"{Tag} {log}");
        }

        /// <summary>Layout-only checkpoint before tiles are painted.</summary>
        public static void LogLayoutCheckpoint(DungeonGenerationContext context, string label)
        {
            if (context == null)
                return;

            var log = new StringBuilder();
            log.Append(label);
            log.Append($" floorId={context.Definition?.FloorId}");
            log.Append($" playerStart={context.PlayerStart}");
            log.Append($" layout={context.Definition?.LayoutMode}");
            log.Append($" map={context.MapWidth}x{context.MapHeight}");
            log.Append($" safeZone={context.SafeZoneCells.Count}");

            if (context.ZoneCellMap != null)
                log.Append(' ').Append(ZoneCellMapStats.FormatCounts(ZoneCellMapStats.CountByZone(context.ZoneCellMap)));

            Debug.Log($"{Tag} {log}");
        }

        public static void LogPaintedTileSamples(
            DungeonGenerationContext context,
            DungeonFloorZoneLayout layout,
            string label)
        {
            MapManager map = MapManager.Instance;
            if (map?.FloorMap == null || context == null || layout == null)
                return;

            Vector3Int[] samples =
            {
                new Vector3Int(10, 5, 0),
                new Vector3Int(15, 25, 0),
                new Vector3Int(25, 10, 0),
                new Vector3Int(28, 10, 0),
            };

            var log = new StringBuilder();
            log.Append(label).Append(" paintedFloorSamples=[");
            for (int i = 0; i < samples.Length; i++)
            {
                Vector3Int cell = samples[i];
                if (i > 0)
                    log.Append(", ");

                TileBase tile = map.FloorMap.GetTile(cell);
                context.TryGetZoneId(cell, out string zoneId);
                bool walkable = map.IsWalkable(cell);
                log.Append('(')
                    .Append(cell.x)
                    .Append(',')
                    .Append(cell.y)
                    .Append(" zone=")
                    .Append(string.IsNullOrEmpty(zoneId) ? "?" : zoneId)
                    .Append(" walkable=")
                    .Append(walkable)
                    .Append(" tile=")
                    .Append(tile != null ? tile.name : "none")
                    .Append(')');
            }

            log.Append(']');
            Debug.Log($"{Tag} {log}");
        }

        public static void LogBoundaries(IReadOnlyList<ResolvedZoneBoundary> boundaries)
        {
            if (boundaries == null || boundaries.Count == 0)
            {
                Debug.Log($"{Tag} boundaries=(none)");
                return;
            }

            var log = new StringBuilder();
            log.Append($"boundaries count={boundaries.Count} [");
            for (int i = 0; i < boundaries.Count; i++)
            {
                ResolvedZoneBoundary boundary = boundaries[i];
                ZoneInterface iface = boundary.Interface;
                if (i > 0)
                    log.Append("; ");

                log.Append(iface.PieceAId)
                    .Append('(')
                    .Append(iface.EdgeOnA)
                    .Append('@')
                    .Append(iface.FixedCoordOnA)
                    .Append(')')
                    .Append("->")
                    .Append(iface.PieceBId)
                    .Append('=')
                    .Append(boundary.Kind)
                    .Append("[span ")
                    .Append(iface.SpanMin)
                    .Append('-')
                    .Append(iface.SpanMax)
                    .Append(']');
            }

            log.Append(']');
            Debug.Log($"{Tag} {log}");
        }

        public static void LogSubStampFill(
            ResolvedZonePiece piece,
            DungeonLayoutStamp stamp,
            int offsetX,
            int offsetY,
            int skippedBorderWalls)
        {
            Debug.Log(
                $"{Tag} SubStamp piece={piece.PieceId} zone={piece.ZoneId} " +
                $"stamp={stamp.name} {stamp.Width}x{stamp.Height} offset=({offsetX},{offsetY}) " +
                $"bounds={piece.Bounds} skippedBorderWalls={skippedBorderWalls}");
        }

        public static void LogPopulationByZone(
            string phaseName,
            MapManager map,
            DungeonGenerationContext context)
        {
            if (map == null || context == null)
                return;

            List<Vector3Int> candidates = PopulationPlacementUtility.CollectFloorCandidates(map, context);
            var byZone = new Dictionary<string, int>();
            for (int i = 0; i < candidates.Count; i++)
            {
                Vector3Int cell = candidates[i];
                if (!context.TryGetZoneId(cell, out string zoneId) || string.IsNullOrEmpty(zoneId))
                    zoneId = "?";

                if (byZone.TryGetValue(zoneId, out int count))
                    byZone[zoneId] = count + 1;
                else
                    byZone[zoneId] = 1;
            }

            Debug.Log($"{Tag} {phaseName} candidates={candidates.Count} {ZoneCellMapStats.FormatCounts(byZone)}");
        }

        public static void LogZoneInstancePopulationCandidates(
            DungeonGenerationContext context,
            string phaseName)
        {
            if (context == null || !context.UsesZoneComposite)
                return;

            MapManager map = MapManager.Instance;
            if (map == null)
                return;

            IReadOnlyList<ResolvedZonePiece> instances = ZonePopulationUtility.GetHabitatInstances(context);
            if (instances.Count == 0)
                return;

            var log = new StringBuilder();
            log.Append(phaseName).Append(" zoneInstances=[");
            DungeonFloorZoneLayout layout = context.Definition?.ZoneLayout;

            for (int i = 0; i < instances.Count; i++)
            {
                ResolvedZonePiece instance = instances[i];
                if (i > 0)
                    log.Append("; ");

                List<Vector3Int> candidates = PopulationPlacementUtility.CollectZoneInstanceCandidates(
                    map,
                    context,
                    instance.ZoneInstanceId);
                int enemyRows = ZonePopulationUtility.ResolveEnemyEntries(
                    context.Definition,
                    layout,
                    instance.ZoneId).Count;
                int itemRows = ZonePopulationUtility.ResolveFloorItemEntries(
                    context.Definition,
                    layout,
                    instance.ZoneId).Count;

                log.Append(instance.ZoneInstanceId)
                    .Append(" candidates=")
                    .Append(candidates.Count)
                    .Append(" enemyRows=")
                    .Append(enemyRows)
                    .Append(" itemRows=")
                    .Append(itemRows);
            }

            log.Append(']');
            Debug.Log($"{Tag} {log}");
        }

        static void CountWalkableByZone(
            MapManager map,
            DungeonGenerationContext context,
            int width,
            int height,
            out Dictionary<string, int> walkableByZone,
            out int walkableTotal)
        {
            walkableByZone = new Dictionary<string, int>();
            walkableTotal = 0;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Vector3Int cell = new Vector3Int(x, y, 0);
                    if (!map.IsWalkable(cell))
                        continue;

                    walkableTotal++;
                    string zoneId = ZoneIds.Rock;
                    if (context.TryGetZoneId(cell, out string mappedZone) && !string.IsNullOrEmpty(mappedZone))
                        zoneId = mappedZone;

                    if (walkableByZone.TryGetValue(zoneId, out int count))
                        walkableByZone[zoneId] = count + 1;
                    else
                        walkableByZone[zoneId] = 1;
                }
            }
        }

        static void AppendReachability(
            DungeonGenerationContext context,
            MapManager map,
            int width,
            int height,
            int walkableTotal,
            StringBuilder log)
        {
            if (walkableTotal <= 0)
            {
                log.Append(" reachableFromStart=0 (map not painted yet)");
                return;
            }

            if (!map.IsWalkable(context.PlayerStart))
            {
                log.Append(" reachableFromStart=0 WARNING:playerStartNotWalkable");
                Debug.LogWarning(
                    $"{Tag} Player start {context.PlayerStart} is not walkable after generation.");
                return;
            }

            if (!context.TryGetZoneId(context.PlayerStart, out string startZone) || string.IsNullOrEmpty(startZone))
                startZone = "?";

            var visited = new HashSet<Vector3Int>();
            var queue = new Queue<Vector3Int>();
            queue.Enqueue(context.PlayerStart);
            visited.Add(context.PlayerStart);

            var reachableZones = new HashSet<string> { startZone };

            while (queue.Count > 0)
            {
                Vector3Int cell = queue.Dequeue();
                if (context.TryGetZoneId(cell, out string zoneId) && !string.IsNullOrEmpty(zoneId))
                    reachableZones.Add(zoneId);

                TryEnqueue(map, cell + Vector3Int.up, visited, queue);
                TryEnqueue(map, cell + Vector3Int.down, visited, queue);
                TryEnqueue(map, cell + Vector3Int.right, visited, queue);
                TryEnqueue(map, cell + Vector3Int.left, visited, queue);
            }

            log.Append($" reachableFromStart={visited.Count} zones=[");
            int i = 0;
            foreach (string zone in reachableZones)
            {
                if (i++ > 0)
                    log.Append(',');

                log.Append(zone);
            }

            log.Append(']');

            if (reachableZones.Count <= 1
                && startZone != ZoneIds.Rock
                && startZone != ZoneIds.Empty
                && HasMultipleHabitatZones(context))
            {
                log.Append(" WARNING:playerIsolatedFromOtherZones");
                Debug.LogWarning($"{Tag} Player at {context.PlayerStart} cannot walk into any other habitat zone.");
            }
        }

        static bool HasMultipleHabitatZones(DungeonGenerationContext context)
        {
            if (context.ResolvedZonePieces == null)
                return false;

            int habitatCount = 0;
            for (int i = 0; i < context.ResolvedZonePieces.Length; i++)
            {
                string zoneId = context.ResolvedZonePieces[i].ZoneId;
                if (zoneId == ZoneIds.Empty || zoneId == ZoneIds.Rock)
                    continue;

                habitatCount++;
                if (habitatCount > 1)
                    return true;
            }

            return false;
        }

        static void TryEnqueue(
            MapManager map,
            Vector3Int cell,
            HashSet<Vector3Int> visited,
            Queue<Vector3Int> queue)
        {
            if (visited.Contains(cell) || !map.IsWalkable(cell))
                return;

            visited.Add(cell);
            queue.Enqueue(cell);
        }
    }
}
