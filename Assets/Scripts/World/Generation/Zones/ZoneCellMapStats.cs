using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace JRogue.World.Generation.Zones
{
    public static class ZoneCellMapStats
    {
        public static Dictionary<string, int> CountByZone(IReadOnlyDictionary<Vector3Int, string> zoneCellMap)
        {
            var counts = new Dictionary<string, int>();
            if (zoneCellMap == null)
                return counts;

            foreach (KeyValuePair<Vector3Int, string> pair in zoneCellMap)
            {
                string zoneId = pair.Value ?? string.Empty;
                if (counts.TryGetValue(zoneId, out int count))
                    counts[zoneId] = count + 1;
                else
                    counts[zoneId] = 1;
            }

            return counts;
        }

        public static string FormatCounts(IReadOnlyDictionary<string, int> counts)
        {
            if (counts == null || counts.Count == 0)
                return "zoneTileCounts=(none)";

            var keys = new List<string>(counts.Keys);
            keys.Sort();

            var log = new StringBuilder();
            log.Append("zoneTileCounts=[");
            for (int i = 0; i < keys.Count; i++)
            {
                if (i > 0)
                    log.Append(", ");

                string zoneId = keys[i];
                log.Append(zoneId).Append('=').Append(counts[zoneId]);
            }

            log.Append(']');
            return log.ToString();
        }
    }

    public struct ZonePaintStats
    {
        public Dictionary<string, int> FloorCellsByZone;
        public Dictionary<string, int> WallCellsByZone;
        public int OuterEdgeWallCells;
        public int MissingZoneDefinitionFloorFallback;
        public int SkippedSubStampBorderWalls;
    }

    public static class ZonePaintStatsFormatter
    {
        public static string Format(in ZonePaintStats stats)
        {
            var log = new StringBuilder();
            log.Append("paintedFloor=").Append(FormatPaintCounts(stats.FloorCellsByZone));
            log.Append(" paintedWall=").Append(FormatPaintCounts(stats.WallCellsByZone));
            log.Append($" outerEdgeWalls={stats.OuterEdgeWallCells}");
            log.Append($" skippedSubStampBorderWalls={stats.SkippedSubStampBorderWalls}");
            log.Append($" floorFallbackNoZoneDef={stats.MissingZoneDefinitionFloorFallback}");
            return log.ToString();
        }

        static string FormatPaintCounts(IReadOnlyDictionary<string, int> counts)
        {
            if (counts == null || counts.Count == 0)
                return "[]";

            var keys = new List<string>(counts.Keys);
            keys.Sort();

            var log = new StringBuilder();
            log.Append('[');
            for (int i = 0; i < keys.Count; i++)
            {
                if (i > 0)
                    log.Append(", ");

                log.Append(keys[i]).Append('=').Append(counts[keys[i]]);
            }

            log.Append(']');
            return log.ToString();
        }
    }
}
