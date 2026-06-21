using System.Collections.Generic;
using JRogue.World.Generation;
using UnityEngine;

namespace JRogue.World.Generation.Zones
{
    public static class ZonePieceFiller
    {
        public static ZonePaintStats FillLayout(
            JRogue.Manager.Map.MapManager map,
            DungeonFloorDefinition floorDef,
            DungeonFloorZoneLayout layout,
            IReadOnlyList<ResolvedZonePiece> pieces,
            IReadOnlyDictionary<Vector3Int, string> zoneCellMap,
            int runSeed,
            string floorId,
            int fillAttempt,
            DungeonLayoutStamp hybridSkeleton,
            IReadOnlyList<ResolvedZoneBoundary> boundaries)
        {
            var stats = new ZonePaintStats
            {
                FloorCellsByZone = new Dictionary<string, int>(),
                WallCellsByZone = new Dictionary<string, int>(),
            };

            if (map == null || layout == null || zoneCellMap == null)
                return stats;

            var paintContext = ZoneTilePaintContext.From(runSeed, floorId);

            map.ClearAllTiles();

            int width = layout.FloorWidth;
            int height = layout.FloorHeight;
            if (layout.LayoutKind == ZoneLayoutKind.Hybrid && hybridSkeleton != null)
                PaintSkeletonBaseline(map, floorDef, layout, hybridSkeleton, zoneCellMap, width, height, paintContext, stats);
            else
                PaintBaseline(map, floorDef, layout, zoneCellMap, width, height, paintContext, stats);

            if (pieces == null)
                return stats;

            bool maskByZoneMap = layout.LayoutKind == ZoneLayoutKind.Hybrid;
            var pieceById = BuildPieceLookup(pieces);
            for (int i = 0; i < pieces.Count; i++)
            {
                ResolvedZonePiece piece = pieces[i];
                if (!IsHabitatZone(piece.ZoneId))
                    continue;

                layout.TryGetZoneDefinition(piece.ZoneId, out DungeonZoneDefinition zoneDef);
                ZoneFillProfile profile = zoneDef != null
                    ? zoneDef.FillProfile
                    : new ZoneFillProfile { mode = ZoneFillMode.SolidRect };

                System.Random fillRng = ZoneGenerationRng.CreateZoneFillRng(runSeed, floorId, piece.PieceId, fillAttempt);
                var openingCells = CollectLocalOpeningCells(piece, pieceById, boundaries);
                FillPiece(
                    map,
                    floorDef,
                    layout,
                    piece,
                    profile,
                    fillRng,
                    zoneCellMap,
                    maskByZoneMap,
                    paintContext,
                    openingCells,
                    stats);
            }

            map.FloorMap?.CompressBounds();
            map.WallMap?.CompressBounds();
            return stats;
        }

        static void PaintBaseline(
            JRogue.Manager.Map.MapManager map,
            DungeonFloorDefinition floorDef,
            DungeonFloorZoneLayout layout,
            IReadOnlyDictionary<Vector3Int, string> zoneCellMap,
            int width,
            int height,
            ZoneTilePaintContext paintContext,
            ZonePaintStats stats)
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Vector3Int cell = new Vector3Int(x, y, 0);
                    bool onOuterEdge = ZoneTilePainter.IsMapOuterEdge(x, y, width, height);

                    if (!zoneCellMap.TryGetValue(cell, out string zoneId))
                        zoneId = layout.FallbackZoneId;

                    if (onOuterEdge || !IsHabitatZone(zoneId))
                    {
                        if (onOuterEdge)
                            stats.OuterEdgeWallCells++;

                        ZoneTilePainter.PaintWall(map, cell, layout, floorDef, zoneId, paintContext);
                        Increment(stats.WallCellsByZone, zoneId);
                    }
                }
            }
        }

        static void PaintSkeletonBaseline(
            JRogue.Manager.Map.MapManager map,
            DungeonFloorDefinition floorDef,
            DungeonFloorZoneLayout layout,
            DungeonLayoutStamp skeleton,
            IReadOnlyDictionary<Vector3Int, string> zoneCellMap,
            int width,
            int height,
            ZoneTilePaintContext paintContext,
            ZonePaintStats stats)
        {
            int stampWidth = Mathf.Min(skeleton.Width, width);
            int stampHeight = Mathf.Min(skeleton.Height, height);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Vector3Int cell = new Vector3Int(x, y, 0);
                    zoneCellMap.TryGetValue(cell, out string zoneId);
                    if (string.IsNullOrEmpty(zoneId))
                        zoneId = layout.FallbackZoneId;

                    if (x >= stampWidth || y >= stampHeight || skeleton.IsWall(x, y) || !skeleton.IsFloor(x, y))
                    {
                        ZoneTilePainter.PaintWall(map, cell, layout, floorDef, zoneId, paintContext);
                        Increment(stats.WallCellsByZone, zoneId);
                        continue;
                    }

                    ZoneTilePainter.PaintFloor(map, cell, layout, floorDef, zoneId, paintContext);
                    Increment(stats.FloorCellsByZone, zoneId);
                }
            }
        }

        static bool CellBelongsToPiece(
            Vector3Int cell,
            ResolvedZonePiece piece,
            IReadOnlyDictionary<Vector3Int, string> zoneCellMap,
            bool maskByZoneMap)
        {
            if (!maskByZoneMap)
                return piece.Bounds.Contains(new Vector2Int(cell.x, cell.y));

            return zoneCellMap.TryGetValue(cell, out string zoneId) && zoneId == piece.ZoneId;
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

        static List<Vector2Int> CollectLocalOpeningCells(
            ResolvedZonePiece piece,
            IReadOnlyDictionary<string, ResolvedZonePiece> pieceById,
            IReadOnlyList<ResolvedZoneBoundary> boundaries)
        {
            var localCells = new List<Vector2Int>();
            if (boundaries == null)
                return localCells;

            RectInt bounds = piece.Bounds;
            for (int i = 0; i < boundaries.Count; i++)
            {
                ResolvedZoneBoundary boundary = boundaries[i];
                if (boundary.Interface.PieceAId != piece.PieceId && boundary.Interface.PieceBId != piece.PieceId)
                    continue;

                pieceById.TryGetValue(boundary.Interface.PieceAId, out ResolvedZonePiece pieceA);
                pieceById.TryGetValue(boundary.Interface.PieceBId, out ResolvedZonePiece pieceB);
                List<Vector3Int> worldCells = ZoneBoundaryOpeningPlanner.CollectOpeningEdgeCells(
                    boundary,
                    piece,
                    boundary.Interface.PieceAId == piece.PieceId ? pieceB : pieceA);

                for (int c = 0; c < worldCells.Count; c++)
                {
                    Vector3Int world = worldCells[c];
                    localCells.Add(new Vector2Int(world.x - bounds.xMin, world.y - bounds.yMin));
                }
            }

            return localCells;
        }

        static void FillPiece(
            JRogue.Manager.Map.MapManager map,
            DungeonFloorDefinition floorDef,
            DungeonFloorZoneLayout layout,
            ResolvedZonePiece piece,
            ZoneFillProfile profile,
            System.Random fillRng,
            IReadOnlyDictionary<Vector3Int, string> zoneCellMap,
            bool maskByZoneMap,
            ZoneTilePaintContext paintContext,
            IReadOnlyList<Vector2Int> localOpeningCells,
            ZonePaintStats stats)
        {
            switch (profile.mode)
            {
                case ZoneFillMode.SubStamp:
                    FillSubStamp(
                        map,
                        floorDef,
                        layout,
                        piece,
                        profile,
                        fillRng,
                        zoneCellMap,
                        maskByZoneMap,
                        paintContext,
                        stats);
                    break;
                case ZoneFillMode.OpenPocket:
                    FillOpenPocket(
                        map,
                        floorDef,
                        layout,
                        piece,
                        profile,
                        fillRng,
                        zoneCellMap,
                        maskByZoneMap,
                        paintContext,
                        stats);
                    break;
                case ZoneFillMode.RoomCorridor:
                    FillFromProcMask(
                        map,
                        floorDef,
                        layout,
                        piece,
                        ApplyOpeningConnections(
                            ZoneRectProcGenerator.GenerateRoomCorridor(
                                piece.Bounds,
                                fillRng,
                                profile,
                                profile.ensureConnectivity),
                            piece.Bounds,
                            localOpeningCells),
                        zoneCellMap,
                        maskByZoneMap,
                        paintContext,
                        stats);
                    break;
                case ZoneFillMode.Cave:
                    FillFromProcMask(
                        map,
                        floorDef,
                        layout,
                        piece,
                        ApplyOpeningConnections(
                            ZoneRectProcGenerator.GenerateCave(
                                piece.Bounds,
                                fillRng,
                                profile.innerWallDensity,
                                profile,
                                profile.ensureConnectivity),
                            piece.Bounds,
                            localOpeningCells),
                        zoneCellMap,
                        maskByZoneMap,
                        paintContext,
                        stats);
                    break;
                default:
                    FillSolidRect(map, floorDef, layout, piece, zoneCellMap, maskByZoneMap, paintContext, stats);
                    break;
            }
        }

        static bool[,] ApplyOpeningConnections(
            bool[,] floorMask,
            RectInt bounds,
            IReadOnlyList<Vector2Int> localOpeningCells)
        {
            if (floorMask == null)
                return null;

            ZoneRectProcGenerator.ConnectOpeningCells(floorMask, bounds, localOpeningCells);
            return floorMask;
        }

        static void FillPiece(
            JRogue.Manager.Map.MapManager map,
            DungeonFloorDefinition floorDef,
            DungeonFloorZoneLayout layout,
            ResolvedZonePiece piece,
            ZoneFillProfile profile,
            System.Random fillRng,
            IReadOnlyDictionary<Vector3Int, string> zoneCellMap,
            bool maskByZoneMap,
            ZoneTilePaintContext paintContext,
            ZonePaintStats stats)
        {
            FillPiece(
                map,
                floorDef,
                layout,
                piece,
                profile,
                fillRng,
                zoneCellMap,
                maskByZoneMap,
                paintContext,
                null,
                stats);
        }

        static void FillFromProcMask(
            JRogue.Manager.Map.MapManager map,
            DungeonFloorDefinition floorDef,
            DungeonFloorZoneLayout layout,
            ResolvedZonePiece piece,
            bool[,] floorMask,
            IReadOnlyDictionary<Vector3Int, string> zoneCellMap,
            bool maskByZoneMap,
            ZoneTilePaintContext paintContext,
            ZonePaintStats stats)
        {
            RectInt bounds = piece.Bounds;
            if (floorMask == null)
            {
                FillSolidRect(map, floorDef, layout, piece, zoneCellMap, maskByZoneMap, paintContext, stats);
                return;
            }

            int width = floorMask.GetLength(0);
            int height = floorMask.GetLength(1);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int worldX = bounds.xMin + x;
                    int worldY = bounds.yMin + y;
                    if (ZoneTilePainter.IsMapOuterEdge(worldX, worldY, layout.FloorWidth, layout.FloorHeight))
                        continue;

                    Vector3Int cell = new Vector3Int(worldX, worldY, 0);
                    if (!CellBelongsToPiece(cell, piece, zoneCellMap, maskByZoneMap))
                        continue;

                    if (floorMask[x, y])
                    {
                        ZoneTilePainter.PaintFloor(map, cell, layout, floorDef, piece.ZoneId, paintContext);
                        Increment(stats.FloorCellsByZone, piece.ZoneId);
                    }
                    else
                    {
                        ZoneTilePainter.PaintWall(map, cell, layout, floorDef, piece.ZoneId, paintContext);
                        Increment(stats.WallCellsByZone, piece.ZoneId);
                    }
                }
            }
        }

        static void FillSolidRect(
            JRogue.Manager.Map.MapManager map,
            DungeonFloorDefinition floorDef,
            DungeonFloorZoneLayout layout,
            ResolvedZonePiece piece,
            IReadOnlyDictionary<Vector3Int, string> zoneCellMap,
            bool maskByZoneMap,
            ZoneTilePaintContext paintContext,
            ZonePaintStats stats)
        {
            RectInt bounds = piece.Bounds;
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                for (int x = bounds.xMin; x < bounds.xMax; x++)
                {
                    Vector3Int cell = new Vector3Int(x, y, 0);
                    if (ZoneTilePainter.IsMapOuterEdge(x, y, layout.FloorWidth, layout.FloorHeight))
                        continue;

                    if (!CellBelongsToPiece(cell, piece, zoneCellMap, maskByZoneMap))
                        continue;

                    ZoneTilePainter.PaintFloor(map, cell, layout, floorDef, piece.ZoneId, paintContext);
                    Increment(stats.FloorCellsByZone, piece.ZoneId);
                }
            }
        }

        static void FillOpenPocket(
            JRogue.Manager.Map.MapManager map,
            DungeonFloorDefinition floorDef,
            DungeonFloorZoneLayout layout,
            ResolvedZonePiece piece,
            ZoneFillProfile profile,
            System.Random fillRng,
            IReadOnlyDictionary<Vector3Int, string> zoneCellMap,
            bool maskByZoneMap,
            ZoneTilePaintContext paintContext,
            ZonePaintStats stats)
        {
            RectInt bounds = piece.Bounds;
            int density = Mathf.Clamp(profile.innerWallDensity, 0, 100);

            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                for (int x = bounds.xMin; x < bounds.xMax; x++)
                {
                    if (ZoneTilePainter.IsMapOuterEdge(x, y, layout.FloorWidth, layout.FloorHeight))
                        continue;

                    Vector3Int cell = new Vector3Int(x, y, 0);
                    if (!CellBelongsToPiece(cell, piece, zoneCellMap, maskByZoneMap))
                        continue;

                    bool interior = x > bounds.xMin && x < bounds.xMax - 1
                        && y > bounds.yMin && y < bounds.yMax - 1;
                    bool placePillar = interior && density > 0 && fillRng.Next(100) < density;

                    if (placePillar)
                    {
                        ZoneTilePainter.PaintWall(map, cell, layout, floorDef, piece.ZoneId, paintContext);
                        Increment(stats.WallCellsByZone, piece.ZoneId);
                    }
                    else
                    {
                        ZoneTilePainter.PaintFloor(map, cell, layout, floorDef, piece.ZoneId, paintContext);
                        Increment(stats.FloorCellsByZone, piece.ZoneId);
                    }
                }
            }
        }

        static void FillSubStamp(
            JRogue.Manager.Map.MapManager map,
            DungeonFloorDefinition floorDef,
            DungeonFloorZoneLayout layout,
            ResolvedZonePiece piece,
            ZoneFillProfile profile,
            System.Random fillRng,
            IReadOnlyDictionary<Vector3Int, string> zoneCellMap,
            bool maskByZoneMap,
            ZoneTilePaintContext paintContext,
            ZonePaintStats stats)
        {
            FillSolidRect(map, floorDef, layout, piece, zoneCellMap, maskByZoneMap, paintContext, stats);

            DungeonLayoutStamp stamp = PickSubStamp(profile.subStampTable, fillRng);
            if (stamp == null)
                return;

            RectInt bounds = piece.Bounds;
            int offsetX = bounds.xMin + (bounds.width - stamp.Width) / 2;
            int offsetY = bounds.yMin + (bounds.height - stamp.Height) / 2;
            int skippedBorderWalls = 0;

            for (int sy = 0; sy < stamp.Height; sy++)
            {
                for (int sx = 0; sx < stamp.Width; sx++)
                {
                    int worldX = offsetX + sx;
                    int worldY = offsetY + sy;
                    if (worldX < bounds.xMin || worldY < bounds.yMin
                        || worldX >= bounds.xMax || worldY >= bounds.yMax)
                    {
                        continue;
                    }

                    if (ZoneTilePainter.IsMapOuterEdge(worldX, worldY, layout.FloorWidth, layout.FloorHeight))
                        continue;

                    Vector3Int cell = new Vector3Int(worldX, worldY, 0);
                    if (!CellBelongsToPiece(cell, piece, zoneCellMap, maskByZoneMap))
                        continue;

                    if (stamp.IsWall(sx, sy))
                    {
                        if (IsSubStampBorderCell(sx, sy, stamp.Width, stamp.Height))
                        {
                            skippedBorderWalls++;
                            stats.SkippedSubStampBorderWalls++;
                            continue;
                        }

                        ZoneTilePainter.PaintWall(map, cell, layout, floorDef, piece.ZoneId, paintContext);
                        Increment(stats.WallCellsByZone, piece.ZoneId);
                    }
                    else if (stamp.IsFloor(sx, sy))
                    {
                        ZoneTilePainter.PaintFloor(map, cell, layout, floorDef, piece.ZoneId, paintContext);
                        Increment(stats.FloorCellsByZone, piece.ZoneId);
                    }
                }
            }

            ZoneGenerationDiagnostics.LogSubStampFill(piece, stamp, offsetX, offsetY, skippedBorderWalls);
        }

        static bool IsSubStampBorderCell(int sx, int sy, int stampWidth, int stampHeight) =>
            sx <= 0 || sy <= 0 || sx >= stampWidth - 1 || sy >= stampHeight - 1;

        static DungeonLayoutStamp PickSubStamp(ZoneSubStampEntry[] table, System.Random rng)
        {
            if (table == null || table.Length == 0)
                return null;

            int totalWeight = 0;
            for (int i = 0; i < table.Length; i++)
            {
                if (table[i].stamp != null && table[i].weight > 0)
                    totalWeight += table[i].weight;
            }

            if (totalWeight <= 0)
                return null;

            int roll = rng.Next(totalWeight);
            for (int i = 0; i < table.Length; i++)
            {
                ZoneSubStampEntry entry = table[i];
                if (entry.stamp == null || entry.weight <= 0)
                    continue;

                roll -= entry.weight;
                if (roll < 0)
                    return entry.stamp;
            }

            return null;
        }

        public static bool TryResolveSubStampPlayerStart(
            ResolvedZonePiece piece,
            ZoneFillProfile profile,
            System.Random fillRng,
            out Vector3Int worldCell)
        {
            worldCell = default;
            if (profile.mode != ZoneFillMode.SubStamp)
                return false;

            DungeonLayoutStamp stamp = PickSubStamp(profile.subStampTable, fillRng);
            if (stamp == null)
                return false;

            if (!stamp.TryGetMarker(StampMarkerIds.PlayerStart, out Vector3Int localCell))
                return false;

            RectInt bounds = piece.Bounds;
            int offsetX = bounds.xMin + (bounds.width - stamp.Width) / 2;
            int offsetY = bounds.yMin + (bounds.height - stamp.Height) / 2;
            worldCell = new Vector3Int(offsetX + localCell.x, offsetY + localCell.y, 0);
            return true;
        }

        public static DungeonLayoutStamp PickSubStampForProfile(ZoneFillProfile profile, System.Random fillRng) =>
            profile.mode == ZoneFillMode.SubStamp
                ? PickSubStamp(profile.subStampTable, fillRng)
                : null;

        static bool IsHabitatZone(string zoneId) =>
            !string.IsNullOrEmpty(zoneId)
            && zoneId != ZoneIds.Empty
            && zoneId != ZoneIds.Rock;

        static void Increment(Dictionary<string, int> counts, string zoneId)
        {
            if (string.IsNullOrEmpty(zoneId))
                zoneId = "(null)";

            if (counts.TryGetValue(zoneId, out int count))
                counts[zoneId] = count + 1;
            else
                counts[zoneId] = 1;
        }
    }
}
