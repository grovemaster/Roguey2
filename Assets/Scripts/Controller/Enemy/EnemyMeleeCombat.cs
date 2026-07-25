using System.Collections.Generic;
using System.Text;
using JRogue.Actors;
using JRogue.Core.Actor;
using JRogue.Manager.Party;
using JRogue.Stats;
using UnityEngine;

namespace JRogue.Controller.Enemy
{
    /// <summary>
    /// Footprint-aware melee targeting and attack profile execution for enemies.
    /// </summary>
    public static class EnemyMeleeCombat
    {
        public static bool IsInMeleeRange(Vector3Int partyCell, EnemyController enemy) =>
            enemy != null && GridFootprintUtility.CanMeleeTargetFootprint(partyCell, enemy);

        public static bool TryExecuteMeleeAttack(EnemyController enemy, PartyManager party)
        {
            if (enemy == null || party == null || party.partyMembers == null)
                return false;

            bool hasSweep = enemy.HasAttackProfile(EnemyAttackProfileKind.AdjacentSideSweep);
            bool hasSingle = enemy.HasAttackProfile(EnemyAttackProfileKind.AdjacentSingle);

            if (hasSweep && TryExecuteSideSweep(enemy, party))
                return true;

            if (hasSingle && TryExecuteSingle(enemy, party))
                return true;

            LogAttackFailed(enemy, party, hasSweep, hasSingle);
            return false;
        }

        /// <summary>
        /// When <see cref="TryExecuteMeleeAttack"/> fails but a member is in footprint melee range,
        /// hit the closest in-range member so Alert turns still deal damage.
        /// </summary>
        public static bool TryExecuteFallbackMelee(EnemyController enemy, PartyManager party)
        {
            BaseActor target = SelectClosestInMeleeRange(enemy, party);
            if (target == null)
                return false;

            Debug.Log(
                $"[ENEMY-MELEE] {enemy.name} fallback single attack → {target.name} " +
                $"(no sweep/single profile match; check attackProfiles on EnemyController).");
            ApplyDamage(enemy, target);
            return true;
        }

        static BaseActor SelectClosestInMeleeRange(EnemyController enemy, PartyManager party)
        {
            if (enemy == null || party?.partyMembers == null)
                return null;

            BaseActor best = null;
            int bestDist = int.MaxValue;
            Vector3Int anchor = enemy.GridPosition;
            for (int i = 0; i < party.partyMembers.Count; i++)
            {
                BaseActor member = party.partyMembers[i];
                if (member == null || !member.gameObject.activeInHierarchy)
                    continue;
                if (!IsInMeleeRange(member.GridPosition, enemy))
                    continue;

                int dist = Mathf.Abs(member.GridPosition.x - anchor.x) + Mathf.Abs(member.GridPosition.y - anchor.y);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = member;
                }
            }

            return best;
        }

        static void LogAttackFailed(EnemyController enemy, PartyManager party, bool hasSweep, bool hasSingle)
        {
            var inRange = new List<string>();
            if (party?.partyMembers != null)
            {
                for (int i = 0; i < party.partyMembers.Count; i++)
                {
                    BaseActor member = party.partyMembers[i];
                    if (member == null || !member.gameObject.activeInHierarchy)
                        continue;
                    if (IsInMeleeRange(member.GridPosition, enemy))
                        inRange.Add($"{member.name}@({member.GridPosition.x},{member.GridPosition.y})");
                }
            }

            string profileNote = !hasSweep && !hasSingle
                ? "attackProfiles is empty — add AdjacentSideSweep and/or AdjacentSingle on EnemyController."
                : "no sweep side bucket and no single target matched profile rules.";

            Debug.LogWarning(
                $"[ENEMY-MELEE] {enemy.name} attack failed: {profileNote} " +
                $"footprint={enemy.footprintWidth}x{enemy.footprintHeight} anchor=({enemy.GridPosition.x},{enemy.GridPosition.y}). " +
                $"In-range party: {(inRange.Count > 0 ? string.Join(", ", inRange) : "none")}.");
        }

        static bool TryExecuteSideSweep(EnemyController enemy, PartyManager party)
        {
            var sideBuckets = new Dictionary<AttackSide, List<BaseActor>>();
            for (int i = 0; i < party.partyMembers.Count; i++)
            {
                BaseActor member = party.partyMembers[i];
                if (member == null || !member.gameObject.activeInHierarchy)
                    continue;

                Vector3Int pos = member.GridPosition;
                if (!GridFootprintUtility.IsManhattanAdjacentToFootprint(pos, enemy))
                    continue;
                if (GridFootprintUtility.IsDiagonalCornerAdjacent(pos, enemy))
                    continue;

                AttackSide side = ClassifySide(enemy, pos);
                if (!sideBuckets.TryGetValue(side, out List<BaseActor> list))
                {
                    list = new List<BaseActor>();
                    sideBuckets[side] = list;
                }

                list.Add(member);
            }

            if (sideBuckets.Count == 0)
                return false;

            AttackSide bestSide = AttackSide.North;
            int bestCount = -1;
            foreach (KeyValuePair<AttackSide, List<BaseActor>> kv in sideBuckets)
            {
                if (kv.Value.Count > bestCount)
                {
                    bestCount = kv.Value.Count;
                    bestSide = kv.Key;
                }
            }

            List<BaseActor> targets = sideBuckets[bestSide];
            if (targets.Count == 0)
                return false;

            int tieCount = 0;
            foreach (KeyValuePair<AttackSide, List<BaseActor>> kv in sideBuckets)
            {
                if (kv.Value.Count == bestCount)
                    tieCount++;
            }

            if (tieCount > 1)
            {
                BaseActor leader = party.GetActiveMember();
                if (leader != null)
                {
                    for (int i = 0; i < targets.Count; i++)
                    {
                        if (targets[i] == leader)
                        {
                            bestSide = ClassifySide(enemy, leader.GridPosition);
                            targets = sideBuckets[bestSide];
                            break;
                        }
                    }
                }
                else
                {
                    bestSide = PickSidePriority(sideBuckets, bestCount);
                    targets = sideBuckets[bestSide];
                }
            }

            Debug.Log($"[ENEMY-MELEE] {enemy.name} side sweep ({bestSide}) → {FormatTargetList(targets)}.");
            for (int i = 0; i < targets.Count; i++)
                ApplyDamage(enemy, targets[i]);

            return true;
        }

        static string FormatTargetList(List<BaseActor> targets)
        {
            if (targets == null || targets.Count == 0)
                return "0 targets";

            var sb = new StringBuilder();
            sb.Append(targets.Count).Append(" target(s): ");
            for (int i = 0; i < targets.Count; i++)
            {
                if (i > 0)
                    sb.Append(", ");
                Vector3Int p = targets[i].GridPosition;
                sb.Append(targets[i].name).Append('@').Append(p.x).Append(',').Append(p.y);
            }

            return sb.ToString();
        }

        static AttackSide PickSidePriority(Dictionary<AttackSide, List<BaseActor>> buckets, int count)
        {
            AttackSide[] order = { AttackSide.North, AttackSide.East, AttackSide.South, AttackSide.West };
            for (int i = 0; i < order.Length; i++)
            {
                if (buckets.TryGetValue(order[i], out List<BaseActor> list) && list.Count == count)
                    return order[i];
            }

            return AttackSide.North;
        }

        static bool TryExecuteSingle(EnemyController enemy, PartyManager party)
        {
            BaseActor target = SelectSingleTarget(enemy, party, diagonalCornersOnly: true);
            if (target == null)
                target = SelectSingleTarget(enemy, party, diagonalCornersOnly: false);
            if (target == null)
                return false;

            Debug.Log($"[ENEMY-MELEE] {enemy.name} single attack → {target.name}.");
            ApplyDamage(enemy, target);
            return true;
        }

        static BaseActor SelectSingleTarget(EnemyController enemy, PartyManager party, bool diagonalCornersOnly)
        {
            BaseActor leader = party.GetActiveMember();
            if (leader != null && leader.gameObject.activeInHierarchy)
            {
                Vector3Int pos = leader.GridPosition;
                bool inRange = diagonalCornersOnly
                    ? GridFootprintUtility.IsDiagonalCornerAdjacent(pos, enemy)
                    : GridFootprintUtility.IsManhattanAdjacentToFootprint(pos, enemy)
                        && !GridFootprintUtility.IsDiagonalCornerAdjacent(pos, enemy);
                if (inRange)
                    return leader;
            }

            BaseActor best = null;
            int bestDist = int.MaxValue;
            Vector3Int anchor = enemy.GridPosition;
            for (int i = 0; i < party.partyMembers.Count; i++)
            {
                BaseActor member = party.partyMembers[i];
                if (member == null || !member.gameObject.activeInHierarchy)
                    continue;

                Vector3Int pos = member.GridPosition;
                bool inRange = diagonalCornersOnly
                    ? GridFootprintUtility.IsDiagonalCornerAdjacent(pos, enemy)
                    : GridFootprintUtility.IsManhattanAdjacentToFootprint(pos, enemy);
                if (!inRange)
                    continue;
                if (!diagonalCornersOnly && GridFootprintUtility.IsDiagonalCornerAdjacent(pos, enemy))
                    continue;

                int dist = Mathf.Abs(pos.x - anchor.x) + Mathf.Abs(pos.y - anchor.y);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = member;
                }
            }

            return best;
        }

        static void ApplyDamage(EnemyController enemy, BaseActor target)
        {
            if (target == null)
                return;
            target.TakeDamage(enemy.attackPower, DamageType.Blunt, ArmorInteraction.Full);
        }

        static AttackSide ClassifySide(EnemyController enemy, Vector3Int targetCell)
        {
            var cells = new List<Vector3Int>(8);
            GridFootprintUtility.GetOccupiedCells(enemy, cells);
            int minX = cells[0].x, maxX = cells[0].x, minY = cells[0].y, maxY = cells[0].y;
            for (int i = 1; i < cells.Count; i++)
            {
                minX = Mathf.Min(minX, cells[i].x);
                maxX = Mathf.Max(maxX, cells[i].x);
                minY = Mathf.Min(minY, cells[i].y);
                maxY = Mathf.Max(maxY, cells[i].y);
            }

            if (targetCell.y > maxY)
                return AttackSide.North;
            if (targetCell.y < minY)
                return AttackSide.South;
            if (targetCell.x > maxX)
                return AttackSide.East;
            return AttackSide.West;
        }

        enum AttackSide
        {
            North,
            South,
            East,
            West,
        }
    }
}
