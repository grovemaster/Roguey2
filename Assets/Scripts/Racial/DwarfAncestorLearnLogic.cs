using System;
using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Stats;
using JRogue.World.Generation;
using UnityEngine;

namespace JRogue.Racial
{
    public sealed class DwarfAncestorFrontierOffer
    {
        public SpiritImprintNodeData Node;
        public bool Selectable;
        public string DisabledReason = string.Empty;
    }

    public static class DwarfAncestorLearnLogic
    {
        public const string RaceDenyMessage = DwarfClanJoinLogic.RaceDenyMessage;
        public const string NotMemberMessage = "You must swear allegiance to this clan before paying respects here.";
        public const string WrongClanMessage = "This altar belongs to another clan. Your ancestors wait elsewhere.";
        public const string NoTreeMessage = "Your Ancestor path is not awakened.";
        public const string CompleteMessage = "The ancestors have no new secrets to reveal on this path.";
        public const string BlockedMessage =
            "The ancestors withhold further techniques. Raise your standing, level, or clan prestige.";

        public static bool IsSpeakerEligibleForAltar(
            BaseActor speaker,
            DwarfClanDefinition clan,
            out DwarfClanMembershipRuntime membership,
            out DwarfAncestorPathRuntime pathRuntime,
            out string rejectLine)
        {
            membership = null;
            pathRuntime = null;
            rejectLine = null;

            if (!DwarfClanJoinLogic.IsSpeakerDwarf(speaker, out _, out rejectLine))
                return false;

            membership = speaker.GetComponent<DwarfClanMembershipRuntime>();
            if (membership == null || !membership.IsAffiliated)
            {
                rejectLine = NotMemberMessage;
                return false;
            }

            if (!membership.MatchesClan(clan))
            {
                rejectLine = WrongClanMessage;
                return false;
            }

            pathRuntime = speaker.GetComponent<DwarfAncestorPathRuntime>();
            if (pathRuntime == null
                || pathRuntime.PatronAncestor == null
                || pathRuntime.PatronAncestor.abilityTree == null)
            {
                rejectLine = NoTreeMessage;
                return false;
            }

            return true;
        }

        public static List<DwarfAncestorFrontierOffer> GetFrontierOffers(
            BaseActor speaker,
            DwarfClanDefinition clan)
        {
            var offers = new List<DwarfAncestorFrontierOffer>();
            if (!IsSpeakerEligibleForAltar(
                    speaker,
                    clan,
                    out DwarfClanMembershipRuntime membership,
                    out DwarfAncestorPathRuntime pathRuntime,
                    out _))
            {
                return offers;
            }

            SpiritImprintGraph graph = pathRuntime.PatronAncestor.abilityTree;
            HashSet<string> learned = BuildLearnedSet(pathRuntime);
            int clanPrestige = DwarfClanWorldState.EnsureInstance().GetPrestige(clan.clanId);
            CharacterStats stats = speaker.GetComponent<CharacterStats>();

            if (graph.nodes == null)
                return offers;

            for (int i = 0; i < graph.nodes.Count; i++)
            {
                SpiritImprintNodeData node = graph.nodes[i];
                if (node == null || string.IsNullOrEmpty(node.nodeId))
                    continue;

                if (learned.Contains(node.nodeId))
                    continue;

                if (string.IsNullOrEmpty(node.parentNodeId) || !learned.Contains(node.parentNodeId))
                    continue;

                if (IsForeclosedByExclusivity(graph, learned, node))
                    continue;

                var offer = new DwarfAncestorFrontierOffer { Node = node };
                offer.Selectable = PassesGates(
                    node,
                    stats,
                    membership.ClanMemberRank,
                    clanPrestige,
                    out offer.DisabledReason);
                offers.Add(offer);
            }

            return offers;
        }

        public static bool HasSelectableOffer(IReadOnlyList<DwarfAncestorFrontierOffer> offers)
        {
            if (offers == null)
                return false;

            for (int i = 0; i < offers.Count; i++)
            {
                if (offers[i]?.Selectable == true)
                    return true;
            }

            return false;
        }

        public static bool CanBeginAltarCeremony(out string denyReason) =>
            SafeZonePolicyService.TryAllowDwarfClanCeremony(out denyReason);

        public static string ResolveNodeTitle(SpiritImprintNodeData node)
        {
            if (node == null)
                return string.Empty;

            if (!string.IsNullOrWhiteSpace(node.displayName))
                return node.displayName.Trim();

            return node.nodeId ?? "Technique";
        }

        public static string BuildOfferBodyText(DwarfClanDefinition clan, IReadOnlyList<DwarfAncestorFrontierOffer> offers)
        {
            string title = clan != null && !string.IsNullOrWhiteSpace(clan.altarFlavorTitle)
                ? clan.altarFlavorTitle.Trim()
                : "The ancestors await your offering.";

            if (offers == null || offers.Count == 0)
                return title;

            return title + "\n\nChoose the technique the ancestors reveal:";
        }

        static HashSet<string> BuildLearnedSet(DwarfAncestorPathRuntime pathRuntime)
        {
            var learned = new HashSet<string>(StringComparer.Ordinal);
            IReadOnlyList<string> path = pathRuntime.ChosenPathNodeIds;
            if (path == null)
                return learned;

            for (int i = 0; i < path.Count; i++)
            {
                if (!string.IsNullOrEmpty(path[i]))
                    learned.Add(path[i]);
            }

            return learned;
        }

        static bool IsForeclosedByExclusivity(
            SpiritImprintGraph graph,
            HashSet<string> learned,
            SpiritImprintNodeData candidate)
        {
            if (candidate.siblingExclusivityGroup == 0 || string.IsNullOrEmpty(candidate.parentNodeId))
                return false;

            List<SpiritImprintNodeData> siblings = graph.GetDirectChildren(candidate.parentNodeId);
            for (int i = 0; i < siblings.Count; i++)
            {
                SpiritImprintNodeData sibling = siblings[i];
                if (sibling == null || sibling == candidate)
                    continue;

                if (sibling.siblingExclusivityGroup != candidate.siblingExclusivityGroup)
                    continue;

                if (learned.Contains(sibling.nodeId))
                    return true;
            }

            return false;
        }

        public static bool PassesGates(
            SpiritImprintNodeData node,
            CharacterStats stats,
            int clanMemberRank,
            int clanPrestige,
            out string failureReason)
        {
            failureReason = null;
            if (node == null)
            {
                failureReason = "Unknown technique.";
                return false;
            }

            int level = stats != null ? stats.level : 1;
            if (level < node.requiredCharacterLevel)
            {
                failureReason =
                    $"Requires character level {node.requiredCharacterLevel} (current {level}).";
                return false;
            }

            if (clanMemberRank < node.requiredClanMemberRank)
            {
                failureReason =
                    $"Requires clan rank {node.requiredClanMemberRank} (current {clanMemberRank}).";
                return false;
            }

            if (clanPrestige < node.requiredClanPrestige)
            {
                failureReason =
                    $"Requires clan prestige {node.requiredClanPrestige} (clan has {clanPrestige}).";
                return false;
            }

            return true;
        }
    }
}
