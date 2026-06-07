using System;
using System.Collections.Generic;
using UnityEngine;

namespace JRogue.World.Generation.Zones
{
    public readonly struct ZoneJigsawAssignment
    {
        public ZoneJigsawAssignment(ZoneLayoutPiece piece, string zoneId)
        {
            Piece = piece;
            ZoneId = zoneId;
        }

        public ZoneLayoutPiece Piece { get; }
        public string ZoneId { get; }
    }

    public static class ZoneJigsawSolver
    {
        const int MaxPlacementAttempts = 64;

        public static bool TryPackPieces(
            DungeonFloorZoneLayout layout,
            IReadOnlyList<ZoneJigsawAssignment> assignments,
            System.Random rng,
            out ResolvedZonePiece[] resolvedPieces)
        {
            resolvedPieces = Array.Empty<ResolvedZonePiece>();
            if (layout == null || assignments == null || assignments.Count == 0 || rng == null)
                return false;

            var ordered = new List<ZoneJigsawAssignment>(assignments);
            ordered.Sort(ComparePlacementOrder);

            var placedBounds = new Dictionary<string, RectInt>(StringComparer.Ordinal);
            var resolved = new List<ResolvedZonePiece>(ordered.Count);

            for (int i = 0; i < ordered.Count; i++)
            {
                ZoneJigsawAssignment assignment = ordered[i];
                ZoneLayoutPiece piece = assignment.Piece;
                if (string.IsNullOrEmpty(piece.pieceId))
                    return false;

                if (TryResolveAuthoringRect(piece, layout.FloorWidth, layout.FloorHeight, out RectInt authoredRect))
                {
                    if (!FitsOnFloor(authoredRect, layout) || OverlapsAny(authoredRect, placedBounds))
                        return false;

                    if (!ValidateConnections(piece, authoredRect, placedBounds))
                        return false;

                    placedBounds[piece.pieceId] = authoredRect;
                    resolved.Add(new ResolvedZonePiece(
                        piece.pieceId,
                        assignment.ZoneId,
                        authoredRect,
                        piece.isPlayerStartPiece));
                    continue;
                }

                if (!TryPlacePiece(layout, piece, assignment.ZoneId, placedBounds, rng, out RectInt bounds))
                    return false;

                placedBounds[piece.pieceId] = bounds;
                resolved.Add(new ResolvedZonePiece(
                    piece.pieceId,
                    assignment.ZoneId,
                    bounds,
                    piece.isPlayerStartPiece));
            }

            resolvedPieces = resolved.ToArray();
            return true;
        }

        static int ComparePlacementOrder(ZoneJigsawAssignment a, ZoneJigsawAssignment b)
        {
            int mandatory = b.Piece.mandatory.CompareTo(a.Piece.mandatory);
            if (mandatory != 0)
                return mandatory;

            int constraintsA = a.Piece.connectsTo?.Length ?? 0;
            int constraintsB = b.Piece.connectsTo?.Length ?? 0;
            return constraintsB.CompareTo(constraintsA);
        }

        static bool TryResolveAuthoringRect(ZoneLayoutPiece piece, int floorWidth, int floorHeight, out RectInt rect)
        {
            rect = default;
            NormalizedRect normalized = piece.normalizedRect;
            if (normalized.xMax <= normalized.xMin || normalized.yMax <= normalized.yMin)
                return false;

            rect = ZoneCompassRectResolver.ResolveRect(normalized, floorWidth, floorHeight);
            return rect.width > 0 && rect.height > 0;
        }

        static bool TryPlacePiece(
            DungeonFloorZoneLayout layout,
            ZoneLayoutPiece piece,
            string zoneId,
            Dictionary<string, RectInt> placedBounds,
            System.Random rng,
            out RectInt bounds)
        {
            bounds = default;
            RectInt size = RollPieceSize(layout, zoneId, rng);

            if (placedBounds.Count == 0)
            {
                bounds = new RectInt(1, 1, size.width, size.height);
                return FitsOnFloor(bounds, layout);
            }

            for (int attempt = 0; attempt < MaxPlacementAttempts; attempt++)
            {
                if (attempt > 0)
                    size = RollPieceSize(layout, zoneId, rng);

                if (TryAttachToConnectedPiece(piece, size, placedBounds, layout, rng, out bounds))
                    return true;
            }

            return false;
        }

        static bool TryAttachToConnectedPiece(
            ZoneLayoutPiece piece,
            RectInt size,
            Dictionary<string, RectInt> placedBounds,
            DungeonFloorZoneLayout layout,
            System.Random rng,
            out RectInt bounds)
        {
            bounds = default;
            string[] connectsTo = piece.connectsTo;
            if (connectsTo == null || connectsTo.Length == 0)
                return TryPlaceAnywhere(size, placedBounds, layout, rng, out bounds);

            var candidates = new List<RectInt>();
            for (int i = 0; i < connectsTo.Length; i++)
            {
                if (!placedBounds.TryGetValue(connectsTo[i], out RectInt neighbor))
                    continue;

                CollectAttachmentCandidates(size, neighbor, candidates);
            }

            if (candidates.Count == 0)
                return TryPlaceAnywhere(size, placedBounds, layout, rng, out bounds);

            Shuffle(candidates, rng);
            for (int i = 0; i < candidates.Count; i++)
            {
                RectInt candidate = candidates[i];
                if (!FitsOnFloor(candidate, layout) || OverlapsAny(candidate, placedBounds))
                    continue;

                bounds = candidate;
                return true;
            }

            return false;
        }

        static bool TryPlaceAnywhere(
            RectInt size,
            Dictionary<string, RectInt> placedBounds,
            DungeonFloorZoneLayout layout,
            System.Random rng,
            out RectInt bounds)
        {
            bounds = default;
            int maxX = layout.FloorWidth - size.width - 1;
            int maxY = layout.FloorHeight - size.height - 1;
            if (maxX < 1 || maxY < 1)
                return false;

            for (int attempt = 0; attempt < MaxPlacementAttempts; attempt++)
            {
                int x = rng.Next(1, maxX + 1);
                int y = rng.Next(1, maxY + 1);
                var candidate = new RectInt(x, y, size.width, size.height);
                if (!FitsOnFloor(candidate, layout) || OverlapsAny(candidate, placedBounds))
                    continue;

                bounds = candidate;
                return true;
            }

            return false;
        }

        static void CollectAttachmentCandidates(RectInt size, RectInt neighbor, List<RectInt> candidates)
        {
            AddIfValid(candidates, new RectInt(neighbor.xMax, neighbor.yMin, size.width, size.height));
            AddIfValid(candidates, new RectInt(neighbor.xMin - size.width, neighbor.yMin, size.width, size.height));
            AddIfValid(candidates, new RectInt(neighbor.xMin, neighbor.yMax, size.width, size.height));
            AddIfValid(candidates, new RectInt(neighbor.xMin, neighbor.yMin - size.height, size.width, size.height));
        }

        static void AddIfValid(List<RectInt> candidates, RectInt candidate)
        {
            if (candidate.width <= 0 || candidate.height <= 0)
                return;

            candidates.Add(candidate);
        }

        static RectInt RollPieceSize(DungeonFloorZoneLayout layout, string zoneId, System.Random rng)
        {
            if (layout.TryGetZoneDefinition(zoneId, out DungeonZoneDefinition definition))
            {
                int minWidth = Mathf.Max(1, definition.MinWidth);
                int minHeight = Mathf.Max(1, definition.MinHeight);
                int maxWidth = Mathf.Max(minWidth, definition.MaxWidth);
                int maxHeight = Mathf.Max(minHeight, definition.MaxHeight);
                int width = rng.Next(minWidth, maxWidth + 1);
                int height = rng.Next(minHeight, maxHeight + 1);
                return new RectInt(0, 0, width, height);
            }

            int fallback = rng.Next(8, 13);
            return new RectInt(0, 0, fallback, fallback);
        }

        static bool ValidateConnections(
            ZoneLayoutPiece piece,
            RectInt bounds,
            Dictionary<string, RectInt> placedBounds)
        {
            string[] connectsTo = piece.connectsTo;
            if (connectsTo == null)
                return true;

            for (int i = 0; i < connectsTo.Length; i++)
            {
                if (!placedBounds.TryGetValue(connectsTo[i], out RectInt neighbor))
                    continue;

                if (!SharesEdge(bounds, neighbor))
                    return false;
            }

            return true;
        }

        public static bool SharesEdge(RectInt a, RectInt b)
        {
            if (a.xMax == b.xMin || b.xMax == a.xMin)
                return OverlapLength(a.yMin, a.yMax, b.yMin, b.yMax) >= 1;

            if (a.yMax == b.yMin || b.yMax == a.yMin)
                return OverlapLength(a.xMin, a.xMax, b.xMin, b.xMax) >= 1;

            return false;
        }

        static int OverlapLength(int aMin, int aMax, int bMin, int bMax) =>
            Mathf.Max(0, Mathf.Min(aMax, bMax) - Mathf.Max(aMin, bMin));

        static bool FitsOnFloor(RectInt bounds, DungeonFloorZoneLayout layout) =>
            bounds.xMin >= 0
            && bounds.yMin >= 0
            && bounds.xMax <= layout.FloorWidth
            && bounds.yMax <= layout.FloorHeight;

        static bool OverlapsAny(RectInt bounds, Dictionary<string, RectInt> placedBounds)
        {
            foreach (KeyValuePair<string, RectInt> entry in placedBounds)
            {
                if (ZoneCompassRectResolver.RectsOverlap(bounds, entry.Value))
                    return true;
            }

            return false;
        }

        static void Shuffle(List<RectInt> list, System.Random rng)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int swapIndex = rng.Next(i + 1);
                (list[i], list[swapIndex]) = (list[swapIndex], list[i]);
            }
        }
    }
}
