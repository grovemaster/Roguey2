using System;
using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Manager.Map;
using JRogue.Manager.Party;
using JRogue.World.Lighting;
using UnityEngine;

namespace JRogue.World.Generation
{
    /// <summary>
    /// Town building facade tiles within party sight range stay visible even when another
    /// building wall cell would block shadow-cast LOS (multi-row hub facades). Fog still
    /// applies: out-of-range facade cells remain unseen; explored memory unchanged.
    /// </summary>
    public static class TownBuildingFacadeSight
    {
        static readonly HashSet<Vector3Int> FacadeCells = new HashSet<Vector3Int>();

        public static void Clear() => FacadeCells.Clear();

        public static void RegisterCell(Vector3Int cell) => FacadeCells.Add(cell);

        public static void AddWithinPartySightRange(
            HashSet<Vector3Int> visible,
            HashSet<Vector3Int> litVisible,
            PartyManager party,
            LightingService lighting,
            MapManager map,
            int fallbackSightRange,
            Func<BaseActor, Vector3Int, int> getSightRange,
            Func<Vector3Int, LightingService, MapManager, bool> isLiveVisible,
            Func<BaseActor, Vector3Int, LightingService, bool> isFullyBright)
        {
            if (visible == null || FacadeCells.Count == 0 || party?.partyMembers == null)
                return;

            foreach (Vector3Int facadeCell in FacadeCells)
            {
                if (visible.Contains(facadeCell))
                    continue;

                BaseActor seeingMember = null;
                for (int i = 0; i < party.partyMembers.Count; i++)
                {
                    BaseActor member = party.partyMembers[i];
                    if (member == null || !member.gameObject.activeInHierarchy)
                        continue;

                    Vector3Int origin = member.GridPosition;
                    int sight = getSightRange != null ? getSightRange(member, origin) : fallbackSightRange;
                    if (ChebyshevDistance(origin, facadeCell) > sight)
                        continue;

                    seeingMember = member;
                    break;
                }

                if (seeingMember == null)
                    continue;

                if (isLiveVisible != null && !isLiveVisible(facadeCell, lighting, map))
                    continue;

                visible.Add(facadeCell);

                if (litVisible == null)
                    continue;

                for (int i = 0; i < party.partyMembers.Count; i++)
                {
                    BaseActor member = party.partyMembers[i];
                    if (member == null || !member.gameObject.activeInHierarchy)
                        continue;

                    Vector3Int origin = member.GridPosition;
                    int sight = getSightRange != null ? getSightRange(member, origin) : fallbackSightRange;
                    if (ChebyshevDistance(origin, facadeCell) > sight)
                        continue;

                    if (isFullyBright != null && isFullyBright(member, facadeCell, lighting))
                    {
                        litVisible.Add(facadeCell);
                        break;
                    }
                }
            }
        }

        static int ChebyshevDistance(Vector3Int a, Vector3Int b) =>
            Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));
    }
}
