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
            if (layout.LayoutKind == ZoneLayoutKind.Hybrid)
            {
                DungeonLayoutStamp skeleton = layout.SkeletonStamp ?? def.LayoutStamp;
                if (skeleton == null)
                {
                    DungeonGenerationLog.Warn(
                        "ZoneLayoutPhase: Hybrid layout missing skeleton stamp; using rectangular zone map.");
                    context.ZoneCellMap = ZoneCellMapBuilder.Build(
                        layout.FloorWidth,
                        layout.FloorHeight,
                        layout.FallbackZoneId,
                        selection.Pieces);
                }
                else
                {
                    context.ZoneCellMap = ZoneHybridCellAssigner.Assign(
                        skeleton,
                        layout.FloorWidth,
                        layout.FloorHeight,
                        layout.FallbackZoneId,
                        selection.Pieces,
                        out ResolvedZonePiece[] rebuiltPieces);
                    context.ResolvedZonePieces = rebuiltPieces;
                }
            }
            else
            {
                context.ZoneCellMap = ZoneCellMapBuilder.Build(
                    layout.FloorWidth,
                    layout.FloorHeight,
                    layout.FallbackZoneId,
                    selection.Pieces);
            }

            context.ZoneBoundsByInstanceId = ZoneCellMapBuilder.BuildZoneBounds(context.ResolvedZonePieces);
            context.ZoneBoundsByZoneId = ZoneCellMapBuilder.BuildZoneBoundsByZoneId(context.ResolvedZonePieces);
            context.MapWidth = layout.FloorWidth;
            context.MapHeight = layout.FloorHeight;

            List<ZoneInterface> interfaces = ZoneInterfaceResolver.ResolveInterfaces(
                context.ResolvedZonePieces,
                layout.FloorWidth,
                layout.FloorHeight);
            System.Random boundaryRng = ZoneGenerationRng.CreateZoneSelectionRng(
                context.RunSeed,
                def.FloorId + "_boundaries");
            context.ResolvedZoneBoundaries = ZoneBoundaryResolver.ResolveAll(
                layout,
                context.ResolvedZonePieces,
                interfaces,
                boundaryRng);

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
            ZoneGenerationDiagnostics.LogLayoutCheckpoint(context, "after ZoneLayoutPhase");
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

                ResolvePlayerStartForPiece(context, piece, def);
                return;
            }

            for (int i = 0; i < pieces.Length; i++)
            {
                ResolvedZonePiece piece = pieces[i];
                if (piece.ZoneId == ZoneIds.Empty)
                    continue;

                ResolvePlayerStartForPiece(context, piece, def);
                return;
            }

            context.PlayerStart = new Vector3Int(
                context.MapWidth / 2,
                context.MapHeight / 4,
                0);
            context.BuildSafeZoneForFloor(def);
        }

        static void ResolvePlayerStartForPiece(
            DungeonGenerationContext context,
            ResolvedZonePiece piece,
            DungeonFloorDefinition def)
        {
            context.PlayerStart = ZoneCompassRectResolver.ResolvePlayerStart(piece.Bounds);

            DungeonFloorZoneLayout layout = def?.ZoneLayout;
            if (layout != null
                && layout.TryGetZoneDefinition(piece.ZoneId, out DungeonZoneDefinition zoneDef))
            {
                ZoneFillProfile profile = zoneDef.FillProfile;
                System.Random fillRng = ZoneGenerationRng.CreateZoneFillRng(
                    context.RunSeed,
                    def.FloorId,
                    piece.PieceId);
                if (ZonePieceFiller.TryResolveSubStampPlayerStart(piece, profile, fillRng, out Vector3Int stampStart))
                    context.PlayerStart = stampStart;
            }

            context.BuildSafeZoneForFloor(def);
        }
    }
}
