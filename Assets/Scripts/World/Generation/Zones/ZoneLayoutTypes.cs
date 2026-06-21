using System;
using JRogue.World.Generation;
using UnityEngine;

namespace JRogue.World.Generation.Zones
{
    public enum ZoneLayoutKind
    {
        CompassSlots = 0,
        ExplicitPieces = 1,
        Hybrid = 2,
    }

    public enum ZoneBoundaryKind
    {
        None = 0,
        Open = 1,
        Wall = 2,
        Corridor = 3,
        Mixed = 4,
    }

    public enum ZoneFillMode
    {
        SolidRect = 0,
        SubStamp = 1,
        OpenPocket = 2,
        RoomCorridor = 3,
        Cave = 4,
        VaultOnly = 5,
    }

    public enum ZonePieceAnchorKind
    {
        NormalizedRect = 0,
        Compass = 1,
    }

    public enum CompassDirection
    {
        Center = 0,
        North = 1,
        South = 2,
        East = 3,
        West = 4,
    }

    /// <summary>Special zone id meaning "leave slot as fallback (rock/void)".</summary>
    public static class ZoneIds
    {
        public const string Empty = "empty";
        public const string Rock = "rock";
        public const string ExteriorNeighbor = "__exterior__";
    }

    [Serializable]
    public struct NormalizedRect
    {
        [Range(0f, 1f)] public float xMin;
        [Range(0f, 1f)] public float yMin;
        [Range(0f, 1f)] public float xMax;
        [Range(0f, 1f)] public float yMax;
    }

    [Serializable]
    public struct ZoneLayoutPieceCandidate
    {
        public string zoneId;
        [Min(0)] public int weight;
    }

    [Serializable]
    public struct ZoneSelectionRule
    {
        public string zoneId;
        [Min(0)] public int weight;
        public bool mandatory;
        public string[] requiresAll;
        public string[] requiresAny;
        public string[] excludes;
        [Min(1)] public int maxInstances;
    }

    [Serializable]
    public struct ZoneSubStampEntry
    {
        public DungeonLayoutStamp stamp;
        [Min(0)] public int weight;
    }

    [Serializable]
    public struct ZoneFillProfile
    {
        public ZoneFillMode mode;
        public ZoneSubStampEntry[] subStampTable;
        [Range(0, 100)] public int innerWallDensity;
        public bool ensureConnectivity;
        [Min(1)] public int minCorridorWidth;
        [Min(1)] public int maxCorridorWidth;
        [Range(1, 12)] public int caSmoothingIterations;
        [Min(1)] public int minRoomSize;
        [Min(1)] public int maxRoomSize;
        [Min(1)] public int maxRoomCount;
    }

    [Serializable]
    public struct ZoneEdgeBoundary
    {
        public string neighborPieceId;
        public ZoneBoundaryKind boundaryKind;
        [Min(1)] public int corridorCount;
        [Min(1)] public int corridorWidth;
        [Min(1)] public int corridorWidthMin;
        [Min(1)] public int corridorWidthMax;
    }

    [Serializable]
    public struct ZoneLayoutPiece
    {
        public string pieceId;
        public ZonePieceAnchorKind anchorKind;
        public CompassDirection compassDirection;
        public NormalizedRect normalizedRect;
        public ZoneLayoutPieceCandidate[] candidates;
        public bool mandatory;
        public string[] connectsTo;
        public ZoneBoundaryKind defaultBoundary;
        public ZoneEdgeBoundary[] edgeBoundaries;
        public bool isPlayerStartPiece;
    }

    public readonly struct ResolvedZonePiece
    {
        public ResolvedZonePiece(
            string pieceId,
            string zoneId,
            RectInt bounds,
            bool isPlayerStartPiece)
        {
            PieceId = pieceId;
            ZoneId = zoneId;
            Bounds = bounds;
            IsPlayerStartPiece = isPlayerStartPiece;
        }

        public string PieceId { get; }
        public string ZoneId { get; }
        public RectInt Bounds { get; }
        public bool IsPlayerStartPiece { get; }
        public string ZoneInstanceId => $"{PieceId}:{ZoneId}";
    }

    public readonly struct ZoneSelectionResult
    {
        public ZoneSelectionResult(ResolvedZonePiece[] pieces, bool success, string failureReason)
        {
            Pieces = pieces ?? Array.Empty<ResolvedZonePiece>();
            Success = success;
            FailureReason = failureReason;
        }

        public ResolvedZonePiece[] Pieces { get; }
        public bool Success { get; }
        public string FailureReason { get; }
    }
}
