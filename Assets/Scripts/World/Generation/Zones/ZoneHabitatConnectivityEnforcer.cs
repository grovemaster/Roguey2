using System.Collections.Generic;
using System.Text;
using JRogue.Manager.Map;
using UnityEngine;

namespace JRogue.World.Generation.Zones
{
    /// <summary>
    /// Validates and repairs walkable links between habitat zones (e.g. luminescent cavern ↔ northern dark).
    /// </summary>
    public static class ZoneHabitatConnectivityEnforcer
    {
        const int DefaultMaxCarveSteps = 96;
        const int MinInteriorWalkableCells = 12;

        static readonly Vector3Int[] CardinalOffsets =
        {
            Vector3Int.up,
            Vector3Int.down,
            Vector3Int.right,
            Vector3Int.left,
        };

        public static bool TryEnsureAllHabitatZonesReachable(
            DungeonGenerationContext context,
            MapManager map,
            out int carvedCells,
            out string summary)
        {
            carvedCells = 0;
            summary = string.Empty;
            if (context == null || map == null || context.ResolvedZonePieces == null)
                return false;

            DungeonFloorDefinition def = context.Definition;
            DungeonFloorZoneLayout layout = def?.ZoneLayout;
            if (layout == null)
                return false;

            HashSet<string> requiredZones = CollectRequiredHabitatZones(context.ResolvedZonePieces);
            if (requiredZones.Count <= 1)
            {
                summary = "single habitat zone";
                return true;
            }

            HashSet<string> reachableZones = CollectReachableHabitatZones(context, map, context.PlayerStart);
            if (ContainsAll(requiredZones, reachableZones))
            {
                summary = FormatZoneSet("alreadyConnected", reachableZones);
                return true;
            }

            ZoneTilePaintContext paintContext = ZoneTilePaintContext.From(context);
            var pieceById = BuildPieceLookup(context.ResolvedZonePieces);

            if (context.ResolvedZoneBoundaries != null)
            {
                for (int i = 0; i < context.ResolvedZoneBoundaries.Count; i++)
                {
                    ResolvedZoneBoundary boundary = context.ResolvedZoneBoundaries[i];
                    if (boundary.Kind != ZoneBoundaryKind.Corridor && boundary.Kind != ZoneBoundaryKind.Mixed)
                        continue;

                    if (!pieceById.TryGetValue(boundary.Interface.PieceAId, out ResolvedZonePiece pieceA))
                        continue;

                    pieceById.TryGetValue(boundary.Interface.PieceBId, out ResolvedZonePiece pieceB);
                    if (!IsHabitatZone(pieceA.ZoneId) || !IsHabitatZone(pieceB.ZoneId))
                        continue;

                    carvedCells += RepairBoundaryOpenings(
                        context,
                        map,
                        layout,
                        def,
                        paintContext,
                        boundary,
                        pieceA,
                        pieceB,
                        DefaultMaxCarveSteps);
                }
            }

            carvedCells += RepairPlayerAccessToBoundaryOpenings(
                context,
                map,
                layout,
                def,
                paintContext,
                pieceById,
                DefaultMaxCarveSteps);

            reachableZones = CollectReachableHabitatZones(context, map, context.PlayerStart);
            if (ContainsAll(requiredZones, reachableZones))
            {
                summary = FormatZoneSet($"carved={carvedCells} connected", reachableZones);
                return true;
            }

            summary = $"missing after carve={carvedCells} have=[{FormatZoneSet(string.Empty, reachableZones)}] need=[{FormatZoneSet(string.Empty, requiredZones)}]";
            return false;
        }

        public static HashSet<string> CollectReachableHabitatZones(
            DungeonGenerationContext context,
            MapManager map,
            Vector3Int start)
        {
            var reachableZones = new HashSet<string>();
            if (context == null || map == null || !map.IsWalkable(start))
                return reachableZones;

            var visited = new HashSet<Vector3Int> { start };
            var queue = new Queue<Vector3Int>();
            queue.Enqueue(start);
            TryAddZone(context, start, reachableZones);

            while (queue.Count > 0)
            {
                Vector3Int cell = queue.Dequeue();
                for (int i = 0; i < CardinalOffsets.Length; i++)
                {
                    Vector3Int next = cell + CardinalOffsets[i];
                    if (!visited.Add(next) || !map.IsWalkable(next))
                        continue;

                    queue.Enqueue(next);
                    TryAddZone(context, next, reachableZones);
                }
            }

            return reachableZones;
        }

        static int RepairPlayerAccessToBoundaryOpenings(
            DungeonGenerationContext context,
            MapManager map,
            DungeonFloorZoneLayout layout,
            DungeonFloorDefinition def,
            ZoneTilePaintContext paintContext,
            IReadOnlyDictionary<string, ResolvedZonePiece> pieceById,
            int maxCarveSteps)
        {
            if (context.ResolvedZoneBoundaries == null || !map.IsWalkable(context.PlayerStart))
                return 0;

            HashSet<Vector3Int> playerReachable = CollectReachableCells(map, context.PlayerStart);
            int carved = 0;

            for (int i = 0; i < context.ResolvedZoneBoundaries.Count; i++)
            {
                ResolvedZoneBoundary boundary = context.ResolvedZoneBoundaries[i];
                if (boundary.Kind != ZoneBoundaryKind.Corridor && boundary.Kind != ZoneBoundaryKind.Mixed)
                    continue;

                if (!pieceById.TryGetValue(boundary.Interface.PieceAId, out ResolvedZonePiece pieceA))
                    continue;

                pieceById.TryGetValue(boundary.Interface.PieceBId, out ResolvedZonePiece pieceB);
                carved += RepairPlayerAccessForPiece(
                    context, map, layout, def, paintContext, boundary, pieceA, pieceB, playerReachable, maxCarveSteps);
                carved += RepairPlayerAccessForPiece(
                    context, map, layout, def, paintContext, boundary, pieceB, pieceA, playerReachable, maxCarveSteps);
            }

            return carved;
        }

        static int RepairPlayerAccessForPiece(
            DungeonGenerationContext context,
            MapManager map,
            DungeonFloorZoneLayout layout,
            DungeonFloorDefinition def,
            ZoneTilePaintContext paintContext,
            ResolvedZoneBoundary boundary,
            ResolvedZonePiece piece,
            ResolvedZonePiece neighbor,
            HashSet<Vector3Int> playerReachable,
            int maxCarveSteps)
        {
            if (!IsHabitatZone(piece.ZoneId))
                return 0;

            List<Vector3Int> openings = ZoneBoundaryOpeningPlanner.CollectOpeningEdgeCells(boundary, piece, neighbor);
            if (openings == null || openings.Count == 0)
                return 0;

            int carved = 0;
            for (int i = 0; i < openings.Count; i++)
            {
                Vector3Int opening = openings[i];
                ZoneTilePainter.PaintFloor(map, opening, layout, def, piece.ZoneId, paintContext);
                if (playerReachable.Contains(opening))
                    continue;

                if (!TryFindNearestReachableInZone(context, playerReachable, piece.ZoneId, opening, out Vector3Int anchor))
                    continue;

                carved += CarveManhattanPath(context, map, layout, def, paintContext, opening, anchor, piece.ZoneId, maxCarveSteps);
            }

            return carved;
        }

        static bool TryFindNearestReachableInZone(
            DungeonGenerationContext context,
            HashSet<Vector3Int> playerReachable,
            string zoneId,
            Vector3Int from,
            out Vector3Int anchor)
        {
            anchor = default;
            int bestDist = int.MaxValue;
            bool found = false;

            foreach (Vector3Int cell in playerReachable)
            {
                if (!BelongsToZone(context, cell, zoneId))
                    continue;

                int dist = Mathf.Abs(cell.x - from.x) + Mathf.Abs(cell.y - from.y);
                if (dist >= bestDist)
                    continue;

                bestDist = dist;
                anchor = cell;
                found = true;
            }

            return found;
        }

        static HashSet<Vector3Int> CollectReachableCells(MapManager map, Vector3Int start)
        {
            var visited = new HashSet<Vector3Int> { start };
            var queue = new Queue<Vector3Int>();
            queue.Enqueue(start);

            while (queue.Count > 0)
            {
                Vector3Int cell = queue.Dequeue();
                for (int i = 0; i < CardinalOffsets.Length; i++)
                {
                    Vector3Int next = cell + CardinalOffsets[i];
                    if (!visited.Add(next) || !map.IsWalkable(next))
                        continue;

                    queue.Enqueue(next);
                }
            }

            return visited;
        }

        static int RepairBoundaryOpenings(
            DungeonGenerationContext context,
            MapManager map,
            DungeonFloorZoneLayout layout,
            DungeonFloorDefinition def,
            ZoneTilePaintContext paintContext,
            ResolvedZoneBoundary boundary,
            ResolvedZonePiece pieceA,
            ResolvedZonePiece pieceB,
            int maxCarveSteps)
        {
            int carved = 0;
            List<Vector3Int> openingsA = ZoneBoundaryOpeningPlanner.CollectOpeningEdgeCells(boundary, pieceA, pieceB);
            List<Vector3Int> openingsB = ZoneBoundaryOpeningPlanner.CollectOpeningEdgeCells(boundary, pieceB, pieceA);

            carved += RepairOpeningSet(context, map, layout, def, paintContext, openingsA, pieceA.ZoneId, maxCarveSteps);
            carved += RepairOpeningSet(context, map, layout, def, paintContext, openingsB, pieceB.ZoneId, maxCarveSteps);
            return carved;
        }

        static int RepairOpeningSet(
            DungeonGenerationContext context,
            MapManager map,
            DungeonFloorZoneLayout layout,
            DungeonFloorDefinition def,
            ZoneTilePaintContext paintContext,
            List<Vector3Int> openings,
            string zoneId,
            int maxCarveSteps)
        {
            if (openings == null || openings.Count == 0)
                return 0;

            int carved = 0;
            for (int i = 0; i < openings.Count; i++)
            {
                Vector3Int opening = openings[i];
                ZoneTilePainter.PaintFloor(map, opening, layout, def, zoneId, paintContext);
                if (IsConnectedToZoneInterior(map, context, opening, zoneId))
                    continue;

                carved += CarvePathToZoneInterior(
                    context,
                    map,
                    layout,
                    def,
                    paintContext,
                    opening,
                    zoneId,
                    maxCarveSteps);
            }

            return carved;
        }

        static int CarvePathToZoneInterior(
            DungeonGenerationContext context,
            MapManager map,
            DungeonFloorZoneLayout layout,
            DungeonFloorDefinition def,
            ZoneTilePaintContext paintContext,
            Vector3Int start,
            string zoneId,
            int maxCarveSteps)
        {
            if (IsConnectedToZoneInterior(map, context, start, zoneId))
                return 0;

            if (TryFindNearestWalkableInZone(map, context, start, zoneId, out Vector3Int target))
                return CarveManhattanPath(context, map, layout, def, paintContext, start, target, zoneId, maxCarveSteps);

            if (context.ZoneBoundsByZoneId != null
                && context.ZoneBoundsByZoneId.TryGetValue(zoneId, out RectInt bounds))
            {
                Vector3Int center = new Vector3Int(
                    bounds.xMin + bounds.width / 2,
                    bounds.yMin + bounds.height / 2,
                    0);
                return CarveManhattanPath(context, map, layout, def, paintContext, start, center, zoneId, maxCarveSteps);
            }

            return 0;
        }

        static int CarveManhattanPath(
            DungeonGenerationContext context,
            MapManager map,
            DungeonFloorZoneLayout layout,
            DungeonFloorDefinition def,
            ZoneTilePaintContext paintContext,
            Vector3Int from,
            Vector3Int to,
            string zoneId,
            int maxCarveSteps)
        {
            int carved = 0;
            Vector3Int cursor = from;
            while (carved < maxCarveSteps && cursor != to)
            {
                if (cursor.x != to.x)
                    cursor = new Vector3Int(cursor.x + (cursor.x < to.x ? 1 : -1), cursor.y, 0);
                else if (cursor.y != to.y)
                    cursor = new Vector3Int(cursor.x, cursor.y + (cursor.y < to.y ? 1 : -1), 0);

                if (!BelongsToZone(context, cursor, zoneId))
                    break;

                if (map.IsWalkable(cursor))
                    continue;

                ZoneTilePainter.PaintFloor(map, cursor, layout, def, zoneId, paintContext);
                carved++;
            }

            return carved;
        }

        static bool IsConnectedToZoneInterior(
            MapManager map,
            DungeonGenerationContext context,
            Vector3Int start,
            string zoneId)
        {
            if (!map.IsWalkable(start) || !BelongsToZone(context, start, zoneId))
                return false;

            var visited = new HashSet<Vector3Int> { start };
            var queue = new Queue<Vector3Int>();
            queue.Enqueue(start);

            while (queue.Count > 0)
            {
                Vector3Int cell = queue.Dequeue();
                for (int i = 0; i < CardinalOffsets.Length; i++)
                {
                    Vector3Int next = cell + CardinalOffsets[i];
                    if (!visited.Add(next) || !map.IsWalkable(next) || !BelongsToZone(context, next, zoneId))
                        continue;

                    queue.Enqueue(next);
                }
            }

            return visited.Count >= MinInteriorWalkableCells;
        }

        static bool TryFindNearestWalkableInZone(
            MapManager map,
            DungeonGenerationContext context,
            Vector3Int from,
            string zoneId,
            out Vector3Int target)
        {
            target = default;
            if (context.ZoneBoundsByZoneId == null
                || !context.ZoneBoundsByZoneId.TryGetValue(zoneId, out RectInt bounds))
            {
                return false;
            }

            int bestDist = int.MaxValue;
            bool found = false;
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                for (int x = bounds.xMin; x < bounds.xMax; x++)
                {
                    Vector3Int cell = new Vector3Int(x, y, 0);
                    if (!map.IsWalkable(cell) || !BelongsToZone(context, cell, zoneId))
                        continue;

                    int dist = Mathf.Abs(cell.x - from.x) + Mathf.Abs(cell.y - from.y);
                    if (dist >= bestDist)
                        continue;

                    bestDist = dist;
                    target = cell;
                    found = true;
                }
            }

            return found;
        }

        static bool BelongsToZone(DungeonGenerationContext context, Vector3Int cell, string zoneId) =>
            context.TryGetZoneId(cell, out string mappedZone) && mappedZone == zoneId;

        static void TryAddZone(DungeonGenerationContext context, Vector3Int cell, HashSet<string> zones)
        {
            if (context.TryGetZoneId(cell, out string zoneId) && IsHabitatZone(zoneId))
                zones.Add(zoneId);
        }

        static HashSet<string> CollectRequiredHabitatZones(IReadOnlyList<ResolvedZonePiece> pieces)
        {
            var zones = new HashSet<string>();
            if (pieces == null)
                return zones;

            for (int i = 0; i < pieces.Count; i++)
            {
                string zoneId = pieces[i].ZoneId;
                if (IsHabitatZone(zoneId))
                    zones.Add(zoneId);
            }

            return zones;
        }

        static Dictionary<string, ResolvedZonePiece> BuildPieceLookup(IReadOnlyList<ResolvedZonePiece> pieces)
        {
            var lookup = new Dictionary<string, ResolvedZonePiece>();
            if (pieces == null)
                return lookup;

            for (int i = 0; i < pieces.Count; i++)
                lookup[pieces[i].PieceId] = pieces[i];

            return lookup;
        }

        static bool ContainsAll(HashSet<string> required, HashSet<string> actual)
        {
            foreach (string zoneId in required)
            {
                if (!actual.Contains(zoneId))
                    return false;
            }

            return true;
        }

        static string FormatZoneSet(string prefix, HashSet<string> zones)
        {
            var builder = new StringBuilder();
            if (!string.IsNullOrEmpty(prefix))
                builder.Append(prefix).Append(' ');

            builder.Append('[');
            int i = 0;
            foreach (string zone in zones)
            {
                if (i++ > 0)
                    builder.Append(',');

                builder.Append(zone);
            }

            builder.Append(']');
            return builder.ToString();
        }

        static bool IsHabitatZone(string zoneId) =>
            !string.IsNullOrEmpty(zoneId)
            && zoneId != ZoneIds.Empty
            && zoneId != ZoneIds.Rock;
    }
}
