using System.Collections.Generic;
using System.Text;
using JRogue.Ability;
using JRogue.Actors;
using JRogue.Item.Essence;
using JRogue.Racial;
using JRogue.Stats;
using UnityEngine;

namespace JRogue.UI.Racial
{
    public enum SpiritImprintCardKind
    {
        Committed,
        ForeclosedGhost
    }

    public sealed class SpiritImprintCardViewModel
    {
        public SpiritImprintCardKind Kind;
        public string NodeId = string.Empty;
        public string Title = string.Empty;
        public string Subtitle = string.Empty;
        public string Description = string.Empty;
        public bool IsRoot;
    }

    public sealed class BarbarianSpiritImprintViewModel
    {
        public const string DefaultBannerText =
            "View only — visit the Shaman Barbarian in town to extend your imprint by one mark.";

        public const string NotAwakenedMessage =
            "Spirit imprint is not awakened on this character.";

        public string BannerText = DefaultBannerText;
        public List<SpiritImprintCardViewModel> Cards = new();

        public static BarbarianSpiritImprintViewModel Build(BaseActor member)
        {
            if (member == null)
                return new BarbarianSpiritImprintViewModel();

            var runtime = member.GetComponent<SpiritImprintRuntime>();
            if (runtime == null || runtime.Graph == null)
            {
                return new BarbarianSpiritImprintViewModel
                {
                    BannerText = string.Empty,
                    Cards = new List<SpiritImprintCardViewModel>()
                };
            }

            return BuildFromPath(runtime.Graph, runtime.ChosenPathNodeIds);
        }

        public static BarbarianSpiritImprintViewModel BuildFromPath(
            SpiritImprintGraph graph,
            IReadOnlyList<string> chosenPath)
        {
            var vm = new BarbarianSpiritImprintViewModel();
            if (graph == null)
                return vm;

            var path = graph.ValidateAndNormalizePath(chosenPath, out _) ??
                       new List<string> { graph.rootNodeId };

            var pathSet = new HashSet<string>(path);
            var foreclosed = ComputeForeclosedGhostIds(graph, pathSet);

            foreach (string nodeId in path)
            {
                if (!graph.TryFindNode(nodeId, out var node))
                    continue;

                vm.Cards.Add(BuildCommittedCard(graph, nodeId, node));

                if (string.IsNullOrEmpty(node.parentNodeId))
                    continue;

                foreach (SpiritImprintNodeData sibling in graph.GetDirectChildren(node.parentNodeId))
                {
                    if (sibling == null || !foreclosed.Contains(sibling.nodeId))
                        continue;

                    if (!pathSet.TryGetChosenSibling(sibling, graph, out var chosenSibling))
                        continue;

                    vm.Cards.Add(BuildGhostCard(sibling, chosenSibling));
                }
            }

            return vm;
        }

        static SpiritImprintCardViewModel BuildCommittedCard(
            SpiritImprintGraph graph,
            string nodeId,
            SpiritImprintNodeData node)
        {
            bool isRoot = nodeId == graph.rootNodeId;
            return new SpiritImprintCardViewModel
            {
                Kind = SpiritImprintCardKind.Committed,
                NodeId = nodeId,
                Title = ResolveDisplayName(node),
                Subtitle = isRoot
                    ? (node.HasGameplayPayload() ? "Root" : "Root — DORMANT")
                    : "ACTIVE",
                Description = BuildCommittedDescription(node),
                IsRoot = isRoot
            };
        }

        static SpiritImprintCardViewModel BuildGhostCard(
            SpiritImprintNodeData ghost,
            SpiritImprintNodeData chosenSibling)
        {
            return new SpiritImprintCardViewModel
            {
                Kind = SpiritImprintCardKind.ForeclosedGhost,
                NodeId = ghost.nodeId,
                Title = ResolveDisplayName(ghost),
                Subtitle = $"Not chosen (exclusive with {ResolveDisplayName(chosenSibling)})",
                Description = "New marks only at the Shaman Barbarian.",
                IsRoot = false
            };
        }

        static HashSet<string> ComputeForeclosedGhostIds(
            SpiritImprintGraph graph,
            HashSet<string> pathSet)
        {
            var foreclosed = new HashSet<string>();
            if (graph?.nodes == null)
                return foreclosed;

            foreach (SpiritImprintNodeData node in graph.nodes)
            {
                if (node == null || pathSet.Contains(node.nodeId))
                    continue;

                if (node.siblingExclusivityGroup == 0)
                    continue;

                if (!HasCommittedExclusiveSibling(graph, pathSet, node))
                    continue;

                foreclosed.Add(node.nodeId);
            }

            return foreclosed;
        }

        static bool HasCommittedExclusiveSibling(
            SpiritImprintGraph graph,
            HashSet<string> pathSet,
            SpiritImprintNodeData candidate)
        {
            foreach (string committedId in pathSet)
            {
                if (!graph.TryFindNode(committedId, out var committed))
                    continue;

                if (committed.parentNodeId != candidate.parentNodeId)
                    continue;

                if (committed.siblingExclusivityGroup != candidate.siblingExclusivityGroup)
                    continue;

                return true;
            }

            return false;
        }

        static string ResolveDisplayName(SpiritImprintNodeData node) =>
            string.IsNullOrWhiteSpace(node.displayName) ? node.nodeId : node.displayName.Trim();

        static string BuildCommittedDescription(SpiritImprintNodeData node)
        {
            var sb = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(node.description))
                sb.AppendLine(node.description.Trim());

            AppendStatSection(sb, node.statModifiers);
            AppendResistanceSection(sb, node.resistanceModifiers);
            AppendPassiveSection(sb, node.passiveEffects);
            AppendActiveSection(sb, node.activeAbilities);

            var text = sb.ToString().Trim();
            return string.IsNullOrEmpty(text) ? "—" : text;
        }

        static void AppendStatSection(StringBuilder sb, List<AttributeModifier> mods)
        {
            if (mods == null || mods.Count == 0)
                return;

            sb.AppendLine("STATS");
            foreach (AttributeModifier mod in mods)
            {
                string sign = mod.value >= 0 ? "+" : string.Empty;
                sb.AppendLine($"• {sign}{mod.value} {mod.attribute}");
            }
        }

        static void AppendResistanceSection(StringBuilder sb, List<DamageResistanceModifier> mods)
        {
            if (mods == null || mods.Count == 0)
                return;

            sb.AppendLine("RESISTANCES");
            foreach (DamageResistanceModifier mod in mods)
            {
                string sign = mod.value >= 0 ? "+" : string.Empty;
                sb.AppendLine($"• {sign}{mod.value}% {mod.type}");
            }
        }

        static void AppendPassiveSection(StringBuilder sb, List<PassiveEffect> passives)
        {
            int count = passives?.Count ?? 0;
            sb.AppendLine($"PASSIVES ({count})");
            if (count == 0)
            {
                sb.AppendLine("— none —");
                return;
            }

            foreach (PassiveEffect passive in passives)
            {
                if (passive == null)
                    continue;

                sb.AppendLine($"• {passive.name}");
                if (!string.IsNullOrWhiteSpace(passive.effectDescription))
                    sb.AppendLine(passive.effectDescription.Trim());
            }
        }

        static void AppendActiveSection(StringBuilder sb, List<AbilityAction> actives)
        {
            int count = actives?.Count ?? 0;
            sb.AppendLine($"ACTIVES ({count})");
            if (count == 0)
            {
                sb.AppendLine("— none —");
                return;
            }

            foreach (AbilityAction active in actives)
            {
                if (active == null)
                    continue;

                sb.AppendLine($"• {active.abilityName}");
                if (!string.IsNullOrWhiteSpace(active.description))
                    sb.AppendLine(active.description.Trim());

                sb.AppendLine(FormatAbilityMeta(active));
                sb.AppendLine("Assign on the ability hotbar to use in combat.");
            }
        }

        static string FormatAbilityMeta(AbilityAction active)
        {
            var parts = new List<string>();
            if (active.soulPowerCost != 0)
                parts.Add($"Soul {active.soulPowerCost}");
            if (active.magicPowerCost != 0)
                parts.Add($"Magic {active.magicPowerCost}");
            if (active.divinePowerCost != 0)
                parts.Add($"Divine {active.divinePowerCost}");
            if (active.cooldownTurns != 0)
                parts.Add($"CD {active.cooldownTurns}");

            return parts.Count == 0 ? string.Empty : string.Join(" · ", parts);
        }
    }

    static class SpiritImprintPathSetExtensions
    {
        public static bool TryGetChosenSibling(
            this HashSet<string> pathSet,
            SpiritImprintNodeData ghost,
            SpiritImprintGraph graph,
            out SpiritImprintNodeData chosenSibling)
        {
            chosenSibling = null;
            foreach (string committedId in pathSet)
            {
                if (!graph.TryFindNode(committedId, out var committed))
                    continue;

                if (committed.parentNodeId != ghost.parentNodeId)
                    continue;

                if (committed.siblingExclusivityGroup != ghost.siblingExclusivityGroup)
                    continue;

                chosenSibling = committed;
                return true;
            }

            return false;
        }
    }

    public static class RacialAbilitiesDefaultCopy
    {
        public static string PlaceholderSubtitle(Race race)
        {
            return race switch
            {
                Race.Barbarian => "Spirit Imprint",
                Race.Dwarf => "Racial abilities — coming soon",
                Race.Elf => "Elemental spirit contracts",
                Race.Tiefling => "Cyborg implants (Fleshmetal grafts)",
                Race.Beastman => "Soul Beast bond",
                Race.Human => "Racial abilities — coming soon",
                _ => "Racial abilities — coming soon"
            };
        }
    }
}
