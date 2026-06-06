using System.Collections.Generic;
using System.Text;
using JRogue.World.Generation.Zones;
using UnityEngine;

namespace JRogue.World.Generation.Phases
{
    public sealed class ZoneLayoutPhase : IDungeonGenerationPhase
    {
        public void Execute(DungeonGenerationContext context)
        {
            DungeonFloorDefinition def = context.Definition;
            DungeonFloorZoneLayout layout = def?.ZoneLayout;
            if (layout == null)
            {
                DungeonGenerationLog.Error("ZoneLayoutPhase: missing zoneLayout on floor definition.");
                return;
            }

            System.Random selectionRng = ZoneGenerationRng.CreateZoneSelectionRng(
                context.RunSeed,
                def.FloorId);

            ZoneSelectionResult selection = ZoneSelectionSolver.Resolve(layout, selectionRng);
            if (!selection.Success)
            {
                DungeonGenerationLog.Error(
                    $"ZoneLayoutPhase: selection failed — {selection.FailureReason}");
                return;
            }

            context.ResolvedZonePieces = selection.Pieces;
            context.ZoneCellMap = ZoneCellMapBuilder.Build(
                layout.FloorWidth,
                layout.FloorHeight,
                layout.FallbackZoneId,
                selection.Pieces);
            context.ZoneBoundsByInstanceId = ZoneCellMapBuilder.BuildZoneBounds(selection.Pieces);
            context.MapWidth = layout.FloorWidth;
            context.MapHeight = layout.FloorHeight;

            Dictionary<string, int> zoneTileCounts = ZoneCellMapStats.CountByZone(context.ZoneCellMap);

            ResolvePlayerStart(context, selection.Pieces, def);

            var log = new StringBuilder();
            log.Append("zones=[");
            for (int i = 0; i < selection.Pieces.Length; i++)
            {
                ResolvedZonePiece piece = selection.Pieces[i];
                if (i > 0)
                    log.Append(", ");

                log.Append(piece.PieceId)
                    .Append(':')
                    .Append(piece.ZoneId)
                    .Append('@')
                    .Append(piece.Bounds);
            }

            log.Append("] ");
            log.Append(ZoneCellMapStats.FormatCounts(zoneTileCounts));
            log.Append($" playerStart={context.PlayerStart}");

            if (!string.IsNullOrEmpty(selection.FailureReason))
            {
                log.Append($" note={selection.FailureReason}");
                DungeonGenerationLog.Warn(
                    $"ZoneLayoutPhase used fallback selection: {selection.FailureReason}");
            }

            DungeonGenerationLog.Phase(nameof(ZoneLayoutPhase), log.ToString());
        }

        static void ResolvePlayerStart(
            DungeonGenerationContext context,
            ResolvedZonePiece[] pieces,
            DungeonFloorDefinition def)
        {
            for (int i = 0; i < pieces.Length; i++)
            {
                ResolvedZonePiece piece = pieces[i];
                if (!piece.IsPlayerStartPiece || piece.ZoneId == ZoneIds.Empty)
                    continue;

                context.PlayerStart = ZoneCompassRectResolver.ResolvePlayerStart(piece.Bounds);
                context.BuildSafeZoneForFloor(def);
                return;
            }

            for (int i = 0; i < pieces.Length; i++)
            {
                ResolvedZonePiece piece = pieces[i];
                if (piece.ZoneId == ZoneIds.Empty)
                    continue;

                context.PlayerStart = ZoneCompassRectResolver.ResolvePlayerStart(piece.Bounds);
                context.BuildSafeZoneForFloor(def);
                return;
            }

            context.PlayerStart = new Vector3Int(
                context.MapWidth / 2,
                context.MapHeight / 4,
                0);
            context.BuildSafeZoneForFloor(def);
        }
    }
}
