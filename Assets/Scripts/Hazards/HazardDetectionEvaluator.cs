using JRogue.Actors;
using JRogue.Manager.Map;
using JRogue.Manager.Party;
using JRogue.Manager.Visibility.Algorithm;
using JRogue.Stats;
using UnityEngine;

namespace JRogue.Hazards
{
    /// <summary>Evaluates passive reveal rules for hidden hazards.</summary>
    public static class HazardDetectionEvaluator
    {
        public static bool CanAnyPartyMemberDetect(
            Vector3Int cell,
            HazardDetectionSettings settings,
            MapManager map)
        {
            if (settings == null || settings.method == HazardDetectionMethod.None)
                return false;

            PartyManager party = PartyManager.Instance;
            if (party == null || party.partyMembers == null)
                return false;

            for (int i = 0; i < party.partyMembers.Count; i++)
            {
                BaseActor member = party.partyMembers[i];
                if (member == null || !member.gameObject.activeInHierarchy)
                    continue;

                if (CanMemberDetect(cell, member, settings, map))
                    return true;
            }

            return false;
        }

        static bool CanMemberDetect(
            Vector3Int cell,
            BaseActor member,
            HazardDetectionSettings settings,
            MapManager map)
        {
            CharacterStats stats = member.stats;
            if (stats == null)
                return false;

            return settings.method switch
            {
                HazardDetectionMethod.PartyStatInRange => MeetsStatInRange(cell, member, stats, settings, map),
                HazardDetectionMethod.PartySkill => MeetsSkillThreshold(cell, member, stats, settings, map),
                _ => false,
            };
        }

        static bool MeetsStatInRange(
            Vector3Int cell,
            BaseActor member,
            CharacterStats stats,
            HazardDetectionSettings settings,
            MapManager map)
        {
            Stat stat = stats.GetStatByType(settings.statType);
            if (stat == null)
                return false;

            int value = stat.GetValue();
            if (value < settings.minimumValue)
                return false;

            if (!settings.requireLineOfSight)
                return true;

            if (map == null)
                return false;

            int range = settings.useStatValueAsRange ? value : settings.fixedRange;
            return HasLineOfSight(member, cell, range, map);
        }

        static bool MeetsSkillThreshold(
            Vector3Int cell,
            BaseActor member,
            CharacterStats stats,
            HazardDetectionSettings settings,
            MapManager map)
        {
            if (!stats.Skills.TryGetValue(settings.skillType, out Stat skill) || skill == null)
                return false;

            if (skill.GetValue() < settings.minimumValue)
                return false;

            if (!settings.requireLineOfSight)
                return true;

            if (map == null)
                return false;

            int range = settings.fixedRange;
            if (settings.useStatValueAsRange)
            {
                Stat rangeStat = stats.GetStatByType(settings.statType);
                range = rangeStat != null ? rangeStat.GetValue() : settings.fixedRange;
            }

            return HasLineOfSight(member, cell, range, map);
        }

        static bool HasLineOfSight(BaseActor observer, Vector3Int cell, int range, MapManager map)
        {
            if (range <= 0)
                return false;

            Vector3Int origin = new Vector3Int(observer.GridPosition.x, observer.GridPosition.y, 0);
            Vector3Int target = new Vector3Int(cell.x, cell.y, 0);
            ShadowCaster.IsOpaque isOpaque = pos => !map.IsWalkable(pos);
            return ShadowCaster.IsVisible(origin, target, range, isOpaque);
        }
    }
}
