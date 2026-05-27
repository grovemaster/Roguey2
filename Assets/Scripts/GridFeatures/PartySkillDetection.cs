using JRogue.Actors;
using JRogue.Manager.Map;
using JRogue.Manager.Party;
using JRogue.Stats;
using UnityEngine;

namespace JRogue.GridFeatures
{
    /// <summary>Party-wide skill threshold checks for hidden grid features (traps, hazards).</summary>
    public static class PartySkillDetection
    {
        public static bool CanAnyPartyMemberMeetSkillThreshold(
            SkillType skillType,
            int minimumValue,
            bool requireLineOfSight,
            Vector3Int targetCell,
            MapManager map)
        {
            PartyManager party = PartyManager.Instance;
            if (party == null || party.partyMembers == null)
                return false;

            for (int i = 0; i < party.partyMembers.Count; i++)
            {
                BaseActor member = party.partyMembers[i];
                if (member == null || !member.gameObject.activeInHierarchy)
                    continue;

                if (MemberMeetsSkillThreshold(
                        member,
                        skillType,
                        minimumValue,
                        requireLineOfSight,
                        targetCell,
                        map))
                {
                    return true;
                }
            }

            return false;
        }

        static bool MemberMeetsSkillThreshold(
            BaseActor member,
            SkillType skillType,
            int minimumValue,
            bool requireLineOfSight,
            Vector3Int targetCell,
            MapManager map)
        {
            CharacterStats stats = member.stats;
            if (stats == null || !stats.Skills.TryGetValue(skillType, out Stat skill) || skill == null)
                return false;

            if (skill.GetValue() < minimumValue)
                return false;

            if (!requireLineOfSight)
                return true;

            if (map == null)
                return false;

            return HasLineOfSight(member.GridPosition, targetCell, skill.GetValue(), map);
        }

        static bool HasLineOfSight(Vector3Int observerCell, Vector3Int targetCell, int range, MapManager map)
        {
            if (range <= 0)
                return false;

            Vector3Int origin = new Vector3Int(observerCell.x, observerCell.y, 0);
            Vector3Int target = new Vector3Int(targetCell.x, targetCell.y, 0);
            Manager.Visibility.Algorithm.ShadowCaster.IsOpaque isOpaque = pos => !map.IsWalkable(pos);
            return Manager.Visibility.Algorithm.ShadowCaster.IsVisible(origin, target, range, isOpaque);
        }
    }
}
