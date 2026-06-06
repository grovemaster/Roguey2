using System;
using System.Collections.Generic;
using UnityEngine;

namespace JRogue.World.Generation.Zones
{
    public static class ZoneSelectionSolver
    {
        const int MaxAttempts = 8;

        public static ZoneSelectionResult Resolve(DungeonFloorZoneLayout layout, System.Random rng)
        {
            if (layout == null)
                return new ZoneSelectionResult(Array.Empty<ResolvedZonePiece>(), false, "layout is null");

            if (layout.Pieces == null || layout.Pieces.Length == 0)
                return new ZoneSelectionResult(Array.Empty<ResolvedZonePiece>(), false, "layout has no pieces");

            for (int attempt = 0; attempt < MaxAttempts; attempt++)
            {
                ZoneSelectionResult result = TryResolveOnce(layout, rng);
                if (result.Success)
                    return result;
            }

            return TryResolveMandatoryOnly(layout);
        }

        static ZoneSelectionResult TryResolveOnce(DungeonFloorZoneLayout layout, System.Random rng)
        {
            var selectedZoneIds = new HashSet<string>(StringComparer.Ordinal);
            var resolvedPieces = new List<ResolvedZonePiece>(layout.Pieces.Length);

            for (int i = 0; i < layout.Pieces.Length; i++)
            {
                ZoneLayoutPiece piece = layout.Pieces[i];
                if (string.IsNullOrEmpty(piece.pieceId))
                    return new ZoneSelectionResult(Array.Empty<ResolvedZonePiece>(), false, "piece missing pieceId");

                RectInt bounds = ZoneCompassRectResolver.ResolvePieceRect(
                    piece,
                    layout.FloorWidth,
                    layout.FloorHeight);

                if (bounds.width <= 0 || bounds.height <= 0)
                    return new ZoneSelectionResult(Array.Empty<ResolvedZonePiece>(), false, $"invalid bounds for {piece.pieceId}");

                for (int j = 0; j < resolvedPieces.Count; j++)
                {
                    if (ZoneCompassRectResolver.RectsOverlap(bounds, resolvedPieces[j].Bounds))
                        return new ZoneSelectionResult(Array.Empty<ResolvedZonePiece>(), false, $"overlap at {piece.pieceId}");
                }

                if (!TryPickZoneForPiece(piece, layout, selectedZoneIds, rng, out string zoneId))
                    return new ZoneSelectionResult(Array.Empty<ResolvedZonePiece>(), false, $"no zone for {piece.pieceId}");

                if (!IsZoneAllowed(zoneId, selectedZoneIds, layout.SelectionRules))
                    return new ZoneSelectionResult(Array.Empty<ResolvedZonePiece>(), false, $"rules rejected {zoneId}");

                if (zoneId != ZoneIds.Empty)
                    selectedZoneIds.Add(zoneId);

                resolvedPieces.Add(new ResolvedZonePiece(
                    piece.pieceId,
                    zoneId,
                    bounds,
                    piece.isPlayerStartPiece));
            }

            if (!ValidateGlobalRules(selectedZoneIds, layout.SelectionRules))
                return new ZoneSelectionResult(Array.Empty<ResolvedZonePiece>(), false, "global rules failed");

            return new ZoneSelectionResult(resolvedPieces.ToArray(), true, null);
        }

        static ZoneSelectionResult TryResolveMandatoryOnly(DungeonFloorZoneLayout layout)
        {
            var selectedZoneIds = new HashSet<string>(StringComparer.Ordinal);
            var resolvedPieces = new List<ResolvedZonePiece>(layout.Pieces.Length);

            for (int i = 0; i < layout.Pieces.Length; i++)
            {
                ZoneLayoutPiece piece = layout.Pieces[i];
                RectInt bounds = ZoneCompassRectResolver.ResolvePieceRect(
                    piece,
                    layout.FloorWidth,
                    layout.FloorHeight);

                string zoneId = piece.mandatory ? PickMandatoryZone(piece) : ZoneIds.Empty;
                if (piece.mandatory && string.IsNullOrEmpty(zoneId))
                    return new ZoneSelectionResult(Array.Empty<ResolvedZonePiece>(), false, $"mandatory piece {piece.pieceId} has no zone");

                if (zoneId != ZoneIds.Empty)
                    selectedZoneIds.Add(zoneId);

                resolvedPieces.Add(new ResolvedZonePiece(
                    piece.pieceId,
                    zoneId,
                    bounds,
                    piece.isPlayerStartPiece));
            }

            return new ZoneSelectionResult(resolvedPieces.ToArray(), true, "mandatory fallback");
        }

        static bool TryPickZoneForPiece(
            ZoneLayoutPiece piece,
            DungeonFloorZoneLayout layout,
            HashSet<string> selectedZoneIds,
            System.Random rng,
            out string zoneId)
        {
            zoneId = null;
            if (piece.mandatory)
            {
                zoneId = PickMandatoryZone(piece);
                return !string.IsNullOrEmpty(zoneId);
            }

            ZoneLayoutPieceCandidate[] candidates = piece.candidates;
            if (candidates == null || candidates.Length == 0)
            {
                zoneId = ZoneIds.Empty;
                return true;
            }

            var pool = new List<ZoneLayoutPieceCandidate>();
            int totalWeight = 0;
            for (int i = 0; i < candidates.Length; i++)
            {
                ZoneLayoutPieceCandidate candidate = candidates[i];
                if (candidate.weight <= 0 || string.IsNullOrEmpty(candidate.zoneId))
                    continue;

                if (!IsZoneAllowed(candidate.zoneId, selectedZoneIds, layout.SelectionRules))
                    continue;

                pool.Add(candidate);
                totalWeight += candidate.weight;
            }

            if (pool.Count == 0 || totalWeight <= 0)
            {
                zoneId = ZoneIds.Empty;
                return true;
            }

            int roll = rng.Next(totalWeight);
            for (int i = 0; i < pool.Count; i++)
            {
                ZoneLayoutPieceCandidate candidate = pool[i];
                roll -= candidate.weight;
                if (roll >= 0)
                    continue;

                zoneId = candidate.zoneId;
                return true;
            }

            zoneId = pool[pool.Count - 1].zoneId;
            return true;
        }

        static string PickMandatoryZone(ZoneLayoutPiece piece)
        {
            ZoneLayoutPieceCandidate[] candidates = piece.candidates;
            if (candidates == null)
                return null;

            for (int i = 0; i < candidates.Length; i++)
            {
                if (!string.IsNullOrEmpty(candidates[i].zoneId) && candidates[i].zoneId != ZoneIds.Empty)
                    return candidates[i].zoneId;
            }

            return null;
        }

        static bool IsZoneAllowed(string zoneId, HashSet<string> selectedZoneIds, ZoneSelectionRule[] rules)
        {
            if (string.IsNullOrEmpty(zoneId) || zoneId == ZoneIds.Empty)
                return true;

            if (rules == null)
                return true;

            for (int i = 0; i < rules.Length; i++)
            {
                ZoneSelectionRule rule = rules[i];
                if (rule.zoneId != zoneId)
                    continue;

                if (rule.excludes != null)
                {
                    for (int e = 0; e < rule.excludes.Length; e++)
                    {
                        if (selectedZoneIds.Contains(rule.excludes[e]))
                            return false;
                    }
                }
            }

            for (int i = 0; i < rules.Length; i++)
            {
                ZoneSelectionRule rule = rules[i];
                if (rule.excludes == null)
                    continue;

                for (int e = 0; e < rule.excludes.Length; e++)
                {
                    if (rule.excludes[e] != zoneId)
                        continue;

                    if (selectedZoneIds.Contains(rule.zoneId))
                        return false;
                }
            }

            return true;
        }

        static bool ValidateGlobalRules(HashSet<string> selectedZoneIds, ZoneSelectionRule[] rules)
        {
            if (rules == null)
                return true;

            for (int i = 0; i < rules.Length; i++)
            {
                ZoneSelectionRule rule = rules[i];
                if (!selectedZoneIds.Contains(rule.zoneId))
                    continue;

                if (rule.requiresAll != null)
                {
                    for (int r = 0; r < rule.requiresAll.Length; r++)
                    {
                        if (!selectedZoneIds.Contains(rule.requiresAll[r]))
                            return false;
                    }
                }

                if (rule.requiresAny != null && rule.requiresAny.Length > 0)
                {
                    bool any = false;
                    for (int r = 0; r < rule.requiresAny.Length; r++)
                    {
                        if (selectedZoneIds.Contains(rule.requiresAny[r]))
                        {
                            any = true;
                            break;
                        }
                    }

                    if (!any)
                        return false;
                }

                if (rule.excludes != null)
                {
                    for (int e = 0; e < rule.excludes.Length; e++)
                    {
                        if (selectedZoneIds.Contains(rule.excludes[e]))
                            return false;
                    }
                }
            }

            return true;
        }
    }
}
