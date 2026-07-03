using System.Collections.Generic;
using JRogue.Manager.Map;
using JRogue.World.Generation.Phases;
using JRogue.World.Generation.Zones;
using UnityEngine;

namespace JRogue.World.Generation
{
    public static class PortalPlacementResolver
    {
        public static int PlaceLegacyOrthogonalEdgePortals(DungeonGenerationContext context)
        {
            DungeonFloorDefinition def = context.Definition;
            if (def == null || def.OrthogonalEdgePortalCount <= 0)
                return 0;

            MapManager map = MapManager.Instance;
            if (map == null)
                return 0;

            if (!TryGetMapDimensions(context, out int mapWidth, out int mapHeight))
                return 0;

            int placed = 0;
            MapEdge[] edges = { MapEdge.South, MapEdge.North, MapEdge.East, MapEdge.West };
            int targetCount = Mathf.Clamp(def.OrthogonalEdgePortalCount, 0, edges.Length);
            DungeonLayoutStamp stamp = context.UsesPaintedZoneMap ? null : def.LayoutStamp;

            for (int i = 0; i < targetCount; i++)
            {
                MapEdge edge = edges[i];
                bool found = context.UsesPaintedZoneMap
                    ? PortalEdgePlacement.TryFindEdgePortalCell(
                        mapWidth,
                        mapHeight,
                        map,
                        edge,
                        def.OrthogonalEdgeInset,
                        out Vector3Int cell)
                    : PortalEdgePlacement.TryFindEdgePortalCell(
                        stamp,
                        map,
                        edge,
                        def.OrthogonalEdgeInset,
                        out cell);
                if (!found)
                    continue;

                if (context.ReservedCells.Contains(cell))
                    continue;

                def.TryGetEdgePortalSpec(edge, out EdgePortalSpec edgeSpec);
                if (string.IsNullOrEmpty(edgeSpec.targetFloorId) && string.IsNullOrEmpty(edgeSpec.portalLinkId))
                {
                    AddResolvedPortal(
                        context,
                        cell,
                        portalLinkId: string.Empty,
                        targetFloorId: string.Empty,
                        listLabel: string.Empty,
                        PortalPlacementRuleKind.OrthogonalMapEdge,
                        edge);
                    context.ReservedCells.Add(cell);
                    placed++;
                    continue;
                }

                AddResolvedPortal(
                    context,
                    cell,
                    edgeSpec.portalLinkId,
                    edgeSpec.targetFloorId,
                    edgeSpec.listLabel,
                    PortalPlacementRuleKind.OrthogonalMapEdge,
                    edge);
                context.ReservedCells.Add(cell);
                placed++;
            }

            return placed;
        }

        public static bool TryPlaceRule(DungeonGenerationContext context, PortalPlacementRule rule)
        {
            if (context == null)
                return false;

            switch (rule.kind)
            {
                case PortalPlacementRuleKind.OrthogonalMapEdge:
                    return TryPlaceOrthogonalRule(context, rule);
                case PortalPlacementRuleKind.FixedStampMarker:
                    return TryPlaceStampRule(context, rule);
                case PortalPlacementRuleKind.TaggedRegionEdge:
                    return TryPlaceTaggedRegionRule(context, rule);
                case PortalPlacementRuleKind.FixedMapRowEdge:
                    return TryPlaceFixedMapRowRule(context, rule);
                default:
                    return false;
            }
        }

        static bool TryPlaceOrthogonalRule(DungeonGenerationContext context, PortalPlacementRule rule)
        {
            MapManager map = MapManager.Instance;
            if (map == null || !TryGetMapDimensions(context, out int mapWidth, out int mapHeight))
                return false;

            int inset = rule.insetFromEdge > 0 ? rule.insetFromEdge : context.Definition.OrthogonalEdgeInset;
            DungeonLayoutStamp stamp = context.UsesPaintedZoneMap ? null : context.Definition.LayoutStamp;
            bool found = context.UsesPaintedZoneMap
                ? PortalEdgePlacement.TryFindEdgePortalCell(mapWidth, mapHeight, map, rule.edge, inset, out Vector3Int cell)
                : PortalEdgePlacement.TryFindEdgePortalCell(stamp, map, rule.edge, inset, out cell);
            if (!found || context.ReservedCells.Contains(cell))
                return false;

            AddResolvedPortal(
                context,
                cell,
                rule.portalLinkId,
                rule.targetFloorId,
                rule.listLabel,
                PortalPlacementRuleKind.OrthogonalMapEdge,
                rule.edge);
            context.ReservedCells.Add(cell);
            return true;
        }

        static bool TryPlaceStampRule(DungeonGenerationContext context, PortalPlacementRule rule)
        {
            Vector3Int cell = ResolveStampPortalCell(context.Definition.LayoutStamp, rule);
            if (cell == InvalidCell())
                return false;

            if (context.ReservedCells.Contains(cell))
                return false;

            AddResolvedPortal(
                context,
                cell,
                rule.portalLinkId,
                rule.targetFloorId,
                rule.listLabel,
                PortalPlacementRuleKind.FixedStampMarker,
                default);
            context.ReservedCells.Add(cell);
            return true;
        }

        static bool TryPlaceTaggedRegionRule(DungeonGenerationContext context, PortalPlacementRule rule)
        {
            MapManager map = MapManager.Instance;
            if (map == null)
                return false;

            List<Vector3Int> candidates = CollectTaggedRegionCandidates(context, map, rule);
            if (candidates.Count == 0)
            {
                DungeonGenerationLog.Warn(
                    $"{nameof(PortalPlacementResolver)}: TaggedRegionEdge found no candidates " +
                    $"(zoneId='{rule.zoneId}', regionTag='{rule.regionTag}').");
                return false;
            }

            Vector3Int start = context.PlayerStart;
            if (!TryPickTaggedRegionCell(candidates, start, rule.metric, context.Rng, out Vector3Int cell))
                return false;

            if (context.ReservedCells.Contains(cell))
                return false;

            AddResolvedPortal(
                context,
                cell,
                rule.portalLinkId,
                rule.targetFloorId,
                rule.listLabel,
                PortalPlacementRuleKind.TaggedRegionEdge,
                default);
            context.ReservedCells.Add(cell);
            return true;
        }

        static bool TryPlaceFixedMapRowRule(DungeonGenerationContext context, PortalPlacementRule rule)
        {
            MapManager map = MapManager.Instance;
            if (map == null)
                return false;

            List<Vector3Int> candidates = CollectFixedMapRowCandidates(context, map, rule);
            if (candidates.Count == 0)
            {
                DungeonGenerationLog.Warn(
                    $"{nameof(PortalPlacementResolver)}: FixedMapRowEdge found no candidates " +
                    $"(zoneId='{rule.zoneId}', row={rule.fixedMapRow}).");
                return false;
            }

            string salt = string.IsNullOrEmpty(rule.rngSalt) ? "portal" : rule.rngSalt;
            System.Random rng = Zones.ZoneGenerationRng.CreatePopulationRng(
                context.RunSeed,
                context.Definition.FloorId + salt);
            Vector3Int cell = candidates[rng.Next(candidates.Count)];

            if (context.ReservedCells.Contains(cell))
                return false;

            AddResolvedPortal(
                context,
                cell,
                rule.portalLinkId,
                rule.targetFloorId,
                rule.listLabel,
                PortalPlacementRuleKind.FixedMapRowEdge,
                default);
            context.ReservedCells.Add(cell);
            return true;
        }

        public static List<Vector3Int> CollectFixedMapRowCandidates(
            DungeonGenerationContext context,
            MapManager map,
            PortalPlacementRule rule)
        {
            var candidates = new List<Vector3Int>();
            if (context == null || map == null || rule.fixedMapRow < 0)
                return candidates;

            if (!PopulationPlacementUtility.TryGetMapBounds(context, out int width, out int height))
                return candidates;

            int y = rule.fixedMapRow;
            if (y >= height)
                return candidates;

            for (int x = 0; x < width; x++)
            {
                Vector3Int cell = new Vector3Int(x, y, 0);
                if (!map.IsWalkable(cell))
                    continue;

                if (context.ReservedCells.Contains(cell))
                    continue;

                if (!string.IsNullOrEmpty(rule.zoneId))
                {
                    if (!context.TryGetZoneId(cell, out string zoneId) || zoneId != rule.zoneId)
                        continue;
                }

                candidates.Add(cell);
            }

            return candidates;
        }

        public static List<Vector3Int> CollectTaggedRegionCandidates(
            DungeonGenerationContext context,
            MapManager map,
            PortalPlacementRule rule)
        {
            var candidates = new List<Vector3Int>();
            if (context == null || map == null)
                return candidates;

            if (!string.IsNullOrEmpty(rule.zoneId))
            {
                List<Vector3Int> zoneCells = PopulationPlacementUtility.CollectZoneCandidates(
                    map,
                    context,
                    rule.zoneId,
                    excludeReserved: true);
                for (int i = 0; i < zoneCells.Count; i++)
                {
                    Vector3Int cell = zoneCells[i];
                    if (!MeetsMinDistance(cell, context.PlayerStart, rule.minChebyshevFromStart))
                        continue;

                    candidates.Add(cell);
                }

                return candidates;
            }

            if (string.IsNullOrEmpty(rule.regionTag))
                return candidates;

            DungeonGenerationLog.Warn(
                $"{nameof(PortalPlacementResolver)}: regionTag '{rule.regionTag}' is not supported until stamp regions exist.");
            return candidates;
        }

        public static bool TryPickTaggedRegionCell(
            List<Vector3Int> candidates,
            Vector3Int start,
            TaggedRegionPortalMetric metric,
            System.Random rng,
            out Vector3Int cell)
        {
            cell = default;
            if (candidates == null || candidates.Count == 0)
                return false;

            int bestScore = int.MinValue;
            var best = new List<Vector3Int>();
            for (int i = 0; i < candidates.Count; i++)
            {
                Vector3Int candidate = candidates[i];
                int score = ScoreCell(candidate, start, metric);
                if (score > bestScore)
                {
                    bestScore = score;
                    best.Clear();
                    best.Add(candidate);
                }
                else if (score == bestScore)
                {
                    best.Add(candidate);
                }
            }

            if (best.Count == 0)
                return false;

            cell = best[rng != null ? rng.Next(best.Count) : 0];
            return true;
        }

        public static int ScoreCell(Vector3Int cell, Vector3Int start, TaggedRegionPortalMetric metric)
        {
            return metric switch
            {
                TaggedRegionPortalMetric.MaxManhattanFromStart =>
                    ManhattanDistance(cell, start),
                TaggedRegionPortalMetric.MaxY => cell.y,
                TaggedRegionPortalMetric.MaxX => cell.x,
                TaggedRegionPortalMetric.MinY => -cell.y,
                TaggedRegionPortalMetric.MinX => -cell.x,
                _ => ManhattanDistance(cell, start),
            };
        }

        static bool MeetsMinDistance(Vector3Int cell, Vector3Int start, int minChebyshev)
        {
            if (minChebyshev <= 0)
                return true;

            return ChebyshevDistance(cell, start) >= minChebyshev;
        }

        static int ManhattanDistance(Vector3Int a, Vector3Int b) =>
            Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);

        static int ChebyshevDistance(Vector3Int a, Vector3Int b) =>
            Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));

        public static Vector3Int ResolveStampPortalCell(DungeonLayoutStamp stamp, PortalPlacementRule rule)
        {
            if (!string.IsNullOrEmpty(rule.portalMarkerId) && stamp != null &&
                stamp.TryGetMarker(rule.portalMarkerId, out Vector3Int markerCell))
            {
                return markerCell;
            }

            if (rule.portalCell != InvalidCell())
                return rule.portalCell;

            return InvalidCell();
        }

        public static Vector3Int ResolveStampPortalCell(DungeonLayoutStamp stamp, DungeonPortalSpec spec)
        {
            if (!string.IsNullOrEmpty(spec.portalMarkerId) && stamp != null &&
                stamp.TryGetMarker(spec.portalMarkerId, out Vector3Int markerCell))
            {
                return markerCell;
            }

            return spec.portalCell;
        }

        static bool TryGetMapDimensions(DungeonGenerationContext context, out int width, out int height) =>
            PopulationPlacementUtility.TryGetMapBounds(context, out width, out height);

        static void AddResolvedPortal(
            DungeonGenerationContext context,
            Vector3Int cell,
            string portalLinkId,
            string targetFloorId,
            string listLabel,
            PortalPlacementRuleKind sourceKind,
            MapEdge edge)
        {
            context.ResolvedPortals.Add(new ResolvedPortalPlacement
            {
                cell = cell,
                portalLinkId = portalLinkId ?? string.Empty,
                targetFloorId = targetFloorId ?? string.Empty,
                listLabel = listLabel ?? string.Empty,
                sourceKind = sourceKind,
                edge = edge,
            });
        }

        static Vector3Int InvalidCell() => new Vector3Int(int.MinValue, int.MinValue, 0);
    }
}
