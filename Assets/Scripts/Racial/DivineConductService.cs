using System;
using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Data.Enemy;
using JRogue.Manager.Party;
using JRogue.Stats.Racial;
using UnityEngine;

namespace JRogue.Racial
{
    public static class DivineConductTriggers
    {
        public const string KillUndead = "Kill.Undead";
        public const string ExploreNewTile = "Explore.NewTile";
        public const string ItemUsePoison = "Item.Use.Poison";
    }

    public static class DivineConductService
    {
        static readonly HashSet<Vector3Int> _exploredCellsThisFloor = new();
        static int _exploreFloorKey;

        static readonly string[] UndeadSpeciesIds =
        {
            "skeleton",
            "giant_skeleton",
            "zombie",
            "ghoul",
            "wraith",
        };

        public static void ResetFloorExploration(int floorKey)
        {
            _exploreFloorKey = floorKey;
            _exploredCellsThisFloor.Clear();
        }

        public static void NotifyEnemyKilled(string speciesId, BaseActor killer)
        {
            if (string.IsNullOrWhiteSpace(speciesId) || killer == null)
                return;

            if (!IsUndeadSpecies(speciesId))
                return;

            ApplyConductToPriest(killer, DivineConductTriggers.KillUndead, DivineConductKind.PietyGain);
        }

        public static void NotifyPartyLeaderMoved(BaseActor leader, Vector3Int cell)
        {
            if (leader == null)
                return;

            cell.z = 0;
            if (_exploredCellsThisFloor.Contains(cell))
                return;

            _exploredCellsThisFloor.Add(cell);
            ApplyConductToPriest(leader, DivineConductTriggers.ExploreNewTile, DivineConductKind.PietyGain);
        }

        public static void NotifyPoisonItemUsed(BaseActor actor)
        {
            if (actor == null)
                return;

            ApplyConductToPriest(actor, DivineConductTriggers.ItemUsePoison, DivineConductKind.Taboo);
        }

        static bool IsUndeadSpecies(string speciesId)
        {
            if (string.IsNullOrWhiteSpace(speciesId))
                return false;

            string trimmed = speciesId.Trim();
            for (int i = 0; i < UndeadSpeciesIds.Length; i++)
            {
                if (string.Equals(UndeadSpeciesIds[i], trimmed, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return trimmed.IndexOf("skeleton", StringComparison.OrdinalIgnoreCase) >= 0
                   || trimmed.IndexOf("undead", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        static void ApplyConductToPriest(BaseActor actor, string triggerId, DivineConductKind expectedKind)
        {
            HumanPriestCovenantRuntime covenant = actor.GetComponent<HumanPriestCovenantRuntime>();
            if (covenant != null && covenant.IsCommittedPriest)
            {
                TryApplyRule(covenant, triggerId, expectedKind);
                return;
            }

            PartyManager party = PartyManager.Instance;
            if (party?.partyMembers == null)
                return;

            for (int i = 0; i < party.partyMembers.Count; i++)
            {
                BaseActor member = party.partyMembers[i];
                if (member == null)
                    continue;

                covenant = member.GetComponent<HumanPriestCovenantRuntime>();
                if (covenant != null && covenant.IsCommittedPriest)
                    TryApplyRule(covenant, triggerId, expectedKind);
            }
        }

        static void TryApplyRule(
            HumanPriestCovenantRuntime covenant,
            string triggerId,
            DivineConductKind expectedKind)
        {
            if (covenant == null || string.IsNullOrWhiteSpace(triggerId))
                return;

            if (!PatronGodCatalogService.TryGetGod(covenant.PatronGodId, out PatronGodDefinition god)
                || god?.conductRules == null)
            {
                return;
            }

            for (int i = 0; i < god.conductRules.Count; i++)
            {
                DivineConductRuleData rule = god.conductRules[i];
                if (rule == null
                    || !string.Equals(rule.triggerId, triggerId, StringComparison.Ordinal)
                    || rule.kind != expectedKind)
                {
                    continue;
                }

                switch (rule.kind)
                {
                    case DivineConductKind.PietyGain:
                        covenant.AddPiety(rule.pietyDelta, rule.conductId, rule.description);
                        break;
                    case DivineConductKind.PietyLoss:
                        covenant.ApplyPietyLoss(rule.pietyDelta, rule.conductId, rule.description);
                        break;
                    case DivineConductKind.Taboo:
                        covenant.ApplyPietyLoss(rule.pietyDelta, rule.conductId, rule.description);
                        covenant.AddPenance(rule.pietyDelta, "Divine taboo violated.");
                        HumanPriestVowService.NotifyPersonalTaboo(covenant.gameObject, triggerId);
                        break;
                }
            }
        }
    }
}
