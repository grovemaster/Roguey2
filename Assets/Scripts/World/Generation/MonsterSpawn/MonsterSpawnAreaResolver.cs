using System.Collections.Generic;
using JRogue.Manager.Map;
using JRogue.World.Generation.Phases;
using JRogue.World.Generation.Zones;
using UnityEngine;

namespace JRogue.World.Generation.MonsterSpawn
{
    public static class MonsterSpawnAreaResolver
    {
        public static List<Vector3Int> CollectAreaCandidates(
            DungeonFloorInstance instance,
            MonsterSpawnAreaBinding binding,
            string zoneInstanceId,
            MapManager map)
        {
            var candidates = new List<Vector3Int>();
            if (instance == null || map == null)
                return candidates;

            var context = BuildPopulationContext(instance);
            switch (binding.kind)
            {
                case MonsterSpawnAreaBindingKind.ZoneInstance:
                    if (!string.IsNullOrEmpty(zoneInstanceId))
                    {
                        candidates = PopulationPlacementUtility.CollectZoneInstanceCandidates(
                            map,
                            context,
                            zoneInstanceId);
                    }

                    break;

                case MonsterSpawnAreaBindingKind.ZoneId:
                    if (!string.IsNullOrEmpty(binding.zoneId))
                    {
                        candidates = PopulationPlacementUtility.CollectZoneCandidates(
                            map,
                            context,
                            binding.zoneId);
                    }

                    break;

                case MonsterSpawnAreaBindingKind.StampMarkers:
                    CollectStampMarkerCandidates(instance, binding, candidates);
                    break;
            }

            return candidates;
        }

        static void CollectStampMarkerCandidates(
            DungeonFloorInstance instance,
            MonsterSpawnAreaBinding binding,
            List<Vector3Int> candidates)
        {
            DungeonLayoutStamp stamp = instance.Definition?.LayoutStamp;
            if (stamp == null || binding.markerIds == null)
                return;

            for (int i = 0; i < binding.markerIds.Length; i++)
            {
                if (!stamp.TryGetMarker(binding.markerIds[i], out Vector3Int cell))
                    continue;

                candidates.Add(cell);
            }
        }

        static DungeonGenerationContext BuildPopulationContext(DungeonFloorInstance instance)
        {
            var zoneMap = new Dictionary<Vector3Int, string>();
            IReadOnlyList<ZoneCellMapEntry> snapshot = instance.ZoneCellMapSnapshot;
            for (int i = 0; i < snapshot.Count; i++)
            {
                ZoneCellMapEntry entry = snapshot[i];
                zoneMap[new Vector3Int(entry.x, entry.y, 0)] = entry.zoneId;
            }

            var boundsByInstance = new Dictionary<string, RectInt>();
            IReadOnlyList<ResolvedZonePiece> pieces = instance.ResolvedZonePieces;
            for (int i = 0; i < pieces.Count; i++)
            {
                ResolvedZonePiece piece = pieces[i];
                boundsByInstance[piece.ZoneInstanceId] = piece.Bounds;
            }

            int floorSalt = instance.FloorId?.GetHashCode() ?? 0;
            var context = new DungeonGenerationContext(instance.Definition, instance, 0, floorSalt)
            {
                PlayerStart = instance.PlayerStart,
                ZoneCellMap = zoneMap,
                ZoneBoundsByInstanceId = boundsByInstance,
                ResolvedZonePieces = pieces.Count > 0
                    ? ToArray(pieces)
                    : System.Array.Empty<ResolvedZonePiece>(),
            };

            if (PopulationPlacementUtility.TryGetMapBounds(context, out int width, out int height))
            {
                context.MapWidth = width;
                context.MapHeight = height;
            }

            context.BuildSafeZoneForFloor(instance.Definition);
            return context;
        }

        static ResolvedZonePiece[] ToArray(IReadOnlyList<ResolvedZonePiece> pieces)
        {
            var array = new ResolvedZonePiece[pieces.Count];
            for (int i = 0; i < pieces.Count; i++)
                array[i] = pieces[i];
            return array;
        }
    }
}
