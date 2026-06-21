using System.Collections.Generic;
using JRogue.Manager.Map;
using JRogue.Manager.Party;
using JRogue.World.Generation.Phases;
using JRogue.World.Lighting;
using UnityEngine;

namespace JRogue.World.Generation.Zones
{
    /// <summary>
    /// Re-applies zone ambient + palette emitters after <see cref="LightingService.ResetForActiveFloor"/>.
    /// </summary>
    public static class ZoneCompositeLightingSync
    {
        public static bool Apply(
            LightingService lighting,
            MapManager map,
            DungeonFloorDefinition definition,
            DungeonFloorInstance instance,
            int runSeed)
        {
            if (lighting == null || map == null || definition == null
                || definition.LayoutMode != FloorLayoutMode.ZoneComposite
                || definition.ZoneLayout == null)
            {
                return false;
            }

            Dictionary<Vector3Int, string> zoneMap = BuildZoneMap(instance);
            if (zoneMap == null || zoneMap.Count == 0)
            {
                Debug.LogWarning(
                    "[Lighting:EntryDiag] ZoneCompositeLightingSync skipped — zone cell map snapshot is empty. " +
                    "Floor receivers keep default ambient until zone map is rebuilt.");
                return false;
            }

            string floorId = definition.FloorId;
            int floorSalt = floorId != null ? floorId.GetHashCode() : 0;
            var context = new DungeonGenerationContext(definition, instance, runSeed, floorSalt)
            {
                ZoneCellMap = zoneMap,
                MapWidth = definition.ZoneLayout.FloorWidth,
                MapHeight = definition.ZoneLayout.FloorHeight,
            };

            ApplyZoneAmbientRegionDefaults(lighting, definition.ZoneLayout);

            int zoneAmbientCells = ZoneAmbientApplicator.Apply(context, map, lighting);
            int emitterCells = ZoneTileEmitterApplicator.Apply(context, map, lighting);
            int glowFloorCells = ZoneGlowFloorGapFillApplicator.Apply(context, map, lighting);
            if (zoneAmbientCells > 0 || emitterCells > 0 || glowFloorCells > 0)
            {
                lighting.OnPartyVisionActivity();
                DungeonGenerationLog.Phase(
                    nameof(ZoneCompositeLightingSync),
                    $"zoneAmbientCells={zoneAmbientCells} tileEmitters={emitterCells} glowFloorGapFill={glowFloorCells} " +
                    DescribeZoneAmbientRegions(lighting, definition.ZoneLayout));
                LogLightingDiagnosticsIfEnabled(lighting, map, context, emitterCells);
                return true;
            }

            return false;
        }

        public static void LogLightingDiagnosticsIfEnabled(
            LightingService lighting,
            MapManager map,
            DungeonGenerationContext context,
            int emitterCells)
        {
            if (!VisibilityManager.IsVerboseLightingDiagnosticsEnabled())
                return;

            PartyManager party = PartyManager.Instance;
            Vector3Int origin = party?.partyMembers != null && party.partyMembers.Count > 0
                ? new Vector3Int(party.partyMembers[0].GridPosition.x, party.partyMembers[0].GridPosition.y, 0)
                : Vector3Int.zero;

            int[] recvHistogram = new int[LightLevel.Max + 1];
            int walkableSampled = 0;
            int walkableWithEmit = 0;
            if (PopulationPlacementUtility.TryGetMapBounds(context, out int width, out int height))
            {
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        Vector3Int cell = new Vector3Int(x, y, 0);
                        if (!map.IsWalkable(cell))
                            continue;

                        walkableSampled++;
                        int recv = lighting.GetReceivedLight(cell);
                        int emit = lighting.GetEmitLight(cell);
                        recvHistogram[Mathf.Clamp(recv, 0, LightLevel.Max)]++;
                        if (emit > 0)
                            walkableWithEmit++;
                    }
                }
            }

            Debug.Log(
                $"[Lighting:Diag] ZoneCompositeLightingSync emitters={emitterCells} " +
                $"walkable={walkableSampled} walkableEmitters={walkableWithEmit} " +
                $"recv r0={recvHistogram[0]} r1={recvHistogram[1]} r2={recvHistogram[2]} " +
                $"r3={recvHistogram[3]} r4={recvHistogram[4]} r5={recvHistogram[5]} " +
                $"r6+={recvHistogram[6] + recvHistogram[7] + recvHistogram[8] + recvHistogram[9] + recvHistogram[10]}");

            LogSampleCell("origin", origin, lighting, context);
            LogSampleCell("origin+N", origin + new Vector3Int(0, 1, 0), lighting, context);
            LogSampleCell("origin+E", origin + new Vector3Int(1, 0, 0), lighting, context);
        }

        static void LogSampleCell(
            string label,
            Vector3Int cell,
            LightingService lighting,
            DungeonGenerationContext context)
        {
            context.TryGetZoneId(cell, out string zoneId);
            int emit = lighting.GetEmitLight(cell);
            int recv = lighting.GetReceivedLight(cell);
            bool isEmitterRegistry = lighting.TryGetCellData(cell, out LightCellData data) && data.IsEmitter;
            bool isReceiverRegistry = lighting.TryGetCellData(cell, out data) && data.IsReceiver;
            string registryZone = isReceiverRegistry ? data.ZoneId ?? "(empty)" : "n/a";
            Debug.Log(
                $"[Lighting:Diag] Sync {label} {cell} zone={zoneId ?? "?"} " +
                $"emit={emit} recv={recv} registryEmitter={isEmitterRegistry} " +
                $"registryReceiver={isReceiverRegistry} registryZone={registryZone}");
        }

        public static void ApplyZoneAmbientRegionDefaults(LightingService lighting, DungeonFloorZoneLayout layout)
        {
            if (lighting == null || layout?.ZoneDefinitions == null)
                return;

            var seen = new HashSet<int>();
            DungeonZoneDefinition[] definitions = layout.ZoneDefinitions;
            for (int i = 0; i < definitions.Length; i++)
            {
                DungeonZoneDefinition zoneDef = definitions[i];
                if (zoneDef == null)
                    continue;

                int regionId = zoneDef.AmbientRegionId;
                if (regionId < 0 || !seen.Add(regionId))
                    continue;

                lighting.SetAmbientLight(
                    regionId,
                    zoneDef.DefaultAmbientLight,
                    "zone composite ambient");
            }
        }

        public static string DescribeZoneAmbientRegionsForLog(
            LightingService lighting,
            DungeonFloorZoneLayout layout) =>
            DescribeZoneAmbientRegions(lighting, layout);

        static string DescribeZoneAmbientRegions(LightingService lighting, DungeonFloorZoneLayout layout)
        {
            if (lighting == null || layout?.ZoneDefinitions == null)
                return string.Empty;

            var parts = new List<string>();
            var seen = new HashSet<int>();
            DungeonZoneDefinition[] definitions = layout.ZoneDefinitions;
            for (int i = 0; i < definitions.Length; i++)
            {
                DungeonZoneDefinition zoneDef = definitions[i];
                if (zoneDef == null)
                    continue;

                int regionId = zoneDef.AmbientRegionId;
                if (regionId < 0 || !seen.Add(regionId))
                    continue;

                AmbientRegion region = lighting.GetOrCreateAmbientRegion(regionId);
                parts.Add($"region{regionId}={region.CurrentAmbientLight}");
            }

            return parts.Count > 0 ? $"({string.Join(", ", parts)})" : string.Empty;
        }

        static Dictionary<Vector3Int, string> BuildZoneMap(DungeonFloorInstance instance)
        {
            if (instance == null)
                return null;

            IReadOnlyList<ZoneCellMapEntry> snapshot = instance.ZoneCellMapSnapshot;
            if (snapshot == null || snapshot.Count == 0)
                return null;

            var zoneMap = new Dictionary<Vector3Int, string>(snapshot.Count);
            for (int i = 0; i < snapshot.Count; i++)
            {
                ZoneCellMapEntry entry = snapshot[i];
                zoneMap[new Vector3Int(entry.x, entry.y, 0)] = entry.zoneId;
            }

            return zoneMap;
        }
    }
}
