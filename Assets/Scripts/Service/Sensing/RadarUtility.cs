using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Core.Actor;
using UnityEngine;

namespace JRogue.Service.Sensing
{
    /// <summary>
    /// Wall-penetrating creature detection ("radar"). Stateless utility
    /// mirroring <see cref="HearingUtility"/> / sight utilities.
    ///
    /// - Ignores opacity / line of sight (the entire point).
    /// - Filters by <see cref="EssenceType"/> via bitwise overlap.
    /// - Returns <see cref="RadarBlip"/> values only — no actor references.
    /// </summary>
    public static class RadarUtility
    {
        public static IReadOnlyList<RadarBlip> Pulse(
            BaseActor source,
            int radius,
            EssenceType filter)
        {
            Vector3Int origin = source != null ? source.GridPosition : Vector3Int.zero;
            return Pulse(source, origin, radius, filter);
        }

        public static IReadOnlyList<RadarBlip> Pulse(
            BaseActor source,
            Vector3Int origin,
            int radius,
            EssenceType filter)
        {
            var blips = new List<RadarBlip>();
            if (radius <= 0 || filter == EssenceType.None) return blips;

            BaseActor[] candidates = Object.FindObjectsByType<BaseActor>(FindObjectsInactive.Exclude);
            foreach (BaseActor candidate in candidates)
            {
                if (candidate == null) continue;
                if (source != null && candidate.gameObject == source.gameObject) continue;

                EssenceType type = candidate.EssenceType;
                if ((type & filter) == 0) continue;

                int distance = ChebyshevDistance(origin, candidate.GridPosition);
                if (distance > radius) continue;

                Vector3Int pos = candidate.GridPosition;
                Debug.Log($"[SENSE-RADAR] Radar found {type} at ({pos.x},{pos.y}).");
                blips.Add(new RadarBlip(pos, type));
            }
            return blips;
        }

        private static int ChebyshevDistance(Vector3Int a, Vector3Int b)
        {
            return Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));
        }
    }
}
