using System.Collections.Generic;
using System.Text;
using JRogue.Actors;
using JRogue.Dialog;
using JRogue.Item;
using JRogue.Manager.Inventory;
using JRogue.Manager.Party;
using JRogue.Quest;
using JRogue.Racial;
using JRogue.Shop;
using JRogue.Stats;
using UnityEngine;

namespace JRogue.Racial
{
    public static class SpiritImprintUpgradeLogic
    {
        public const string LogPrefix = "[SpiritImprint]";

        public static string GetPathTailId(SpiritImprintRuntime runtime)
        {
            if (runtime?.Graph == null)
                return null;

            IReadOnlyList<string> path = runtime.ChosenPathNodeIds;
            if (path == null || path.Count == 0)
                return runtime.Graph.rootNodeId;

            return path[path.Count - 1];
        }

        public static IReadOnlyList<SpiritImprintNodeData> GetNextNodeOffers(SpiritImprintRuntime runtime)
        {
            if (runtime?.Graph == null)
                return System.Array.Empty<SpiritImprintNodeData>();

            string tail = GetPathTailId(runtime);
            return runtime.Graph.GetDirectChildren(tail);
        }

        public static bool IsSpeakerEligible(BaseActor speaker, out SpiritImprintRuntime runtime, out string rejectLine)
        {
            runtime = null;
            rejectLine = null;

            if (speaker == null)
            {
                rejectLine = "No speaker.";
                return false;
            }

            CharacterStats stats = speaker.GetComponent<CharacterStats>();
            if (stats == null || stats.race != Race.Barbarian)
            {
                rejectLine = "Hello. You are not a Barbarian.";
                return false;
            }

            runtime = speaker.GetComponent<SpiritImprintRuntime>();
            if (runtime == null || runtime.Graph == null)
            {
                rejectLine = "Your spirit imprint is not awakened.";
                return false;
            }

            return true;
        }

        public static List<BaseActor> OrderPartyMembersForPayment(
            IReadOnlyList<BaseActor> partyMembers,
            BaseActor speaker)
        {
            var ordered = new List<BaseActor>();
            if (speaker != null)
                ordered.Add(speaker);

            if (partyMembers == null)
                return ordered;

            for (int i = 0; i < partyMembers.Count; i++)
            {
                BaseActor member = partyMembers[i];
                if (member != null && member != speaker)
                    ordered.Add(member);
            }

            return ordered;
        }

        public static bool CanAfford(
            SpiritImprintUnlockCost cost,
            IReadOnlyList<BaseActor> partyMembersOrdered,
            GameStoryFlagService flags,
            out string denyReason)
        {
            denyReason = null;

            if (cost.gold > 0 && ShopGoldUtility.GetPartyGoldTotal() < cost.gold)
            {
                denyReason = $"Need {cost.gold} gold.";
                return false;
            }

            if (cost.items != null)
            {
                for (int i = 0; i < cost.items.Length; i++)
                {
                    SpiritImprintItemCost row = cost.items[i];
                    if (row.item == null || row.quantity <= 0)
                        continue;

                    int owned = QuestLogic.CountItemInParty(
                        partyMembersOrdered,
                        row.item,
                        QuestActorRequirementKind.None,
                        default);

                    if (owned < row.quantity)
                    {
                        denyReason = $"Need {row.quantity} × {row.item.itemName}.";
                        return false;
                    }
                }
            }

            if (cost.storyFlags != null && flags != null)
            {
                for (int i = 0; i < cost.storyFlags.Length; i++)
                {
                    SpiritImprintFlagCost row = cost.storyFlags[i];
                    if (string.IsNullOrWhiteSpace(row.flagId))
                        continue;

                    bool actual = flags.IsSet(row.flagId.Trim());
                    if (actual != row.expectedValue)
                    {
                        denyReason = $"Requires flag '{row.flagId.Trim()}'.";
                        return false;
                    }
                }
            }

            return true;
        }

        public static bool TryPayCost(
            SpiritImprintUnlockCost cost,
            IReadOnlyList<BaseActor> partyMembersOrdered,
            GameStoryFlagService flags,
            out string failureReason)
        {
            if (!CanAfford(cost, partyMembersOrdered, flags, out failureReason))
                return false;

            if (cost.gold > 0 && !ShopGoldUtility.TrySpendPartyGold(cost.gold))
            {
                failureReason = "Could not spend gold.";
                return false;
            }

            if (cost.items != null)
            {
                for (int i = 0; i < cost.items.Length; i++)
                {
                    SpiritImprintItemCost row = cost.items[i];
                    if (row.item == null || row.quantity <= 0)
                        continue;

                    int removed = QuestLogic.RemoveMatchingItems(
                        partyMembersOrdered,
                        row.item,
                        row.quantity,
                        acceptGenericStacks: true);

                    if (removed < row.quantity)
                    {
                        if (cost.gold > 0)
                            ShopGoldUtility.AddPartyGold(cost.gold);

                        failureReason = $"Could not remove {row.quantity} × {row.item.itemName}.";
                        return false;
                    }
                }
            }

            return true;
        }

        public static bool IsCostEmpty(SpiritImprintUnlockCost cost)
        {
            if (cost.gold > 0)
                return false;

            if (cost.items != null)
            {
                for (int i = 0; i < cost.items.Length; i++)
                {
                    if (cost.items[i].item != null && cost.items[i].quantity > 0)
                        return false;
                }
            }

            if (cost.storyFlags != null)
            {
                for (int i = 0; i < cost.storyFlags.Length; i++)
                {
                    if (!string.IsNullOrWhiteSpace(cost.storyFlags[i].flagId))
                        return false;
                }
            }

            return true;
        }

        public static string FormatCostLong(SpiritImprintUnlockCost cost, GameStoryFlagService flags)
        {
            if (IsCostEmpty(cost))
                return "Free";

            var parts = new List<string>();
            if (cost.gold > 0)
                parts.Add($"{cost.gold} gold");

            if (cost.items != null)
            {
                for (int i = 0; i < cost.items.Length; i++)
                {
                    SpiritImprintItemCost row = cost.items[i];
                    if (row.item == null || row.quantity <= 0)
                        continue;

                    parts.Add($"{row.item.itemName} ×{row.quantity}");
                }
            }

            if (cost.storyFlags != null)
            {
                for (int i = 0; i < cost.storyFlags.Length; i++)
                {
                    SpiritImprintFlagCost row = cost.storyFlags[i];
                    if (string.IsNullOrWhiteSpace(row.flagId))
                        continue;

                    parts.Add($"Requires: quest flag '{row.flagId.Trim()}'");
                }
            }

            return parts.Count == 0 ? "Free" : string.Join(", ", parts);
        }

        public static string FormatCostShort(SpiritImprintUnlockCost cost)
        {
            if (IsCostEmpty(cost))
                return "Free";

            var parts = new List<string>();
            if (cost.gold > 0)
                parts.Add($"{cost.gold} gold");

            if (cost.items != null)
            {
                for (int i = 0; i < cost.items.Length; i++)
                {
                    SpiritImprintItemCost row = cost.items[i];
                    if (row.item == null || row.quantity <= 0)
                        continue;

                    parts.Add($"{row.item.itemName} ×{row.quantity}");
                }
            }

            if (cost.storyFlags != null)
            {
                for (int i = 0; i < cost.storyFlags.Length; i++)
                {
                    SpiritImprintFlagCost row = cost.storyFlags[i];
                    if (string.IsNullOrWhiteSpace(row.flagId))
                        continue;

                    parts.Add($"flag: {row.flagId.Trim()}");
                }
            }

            return parts.Count == 0 ? "Free" : string.Join(", ", parts);
        }

        public static string BuildOfferBodyText(IReadOnlyList<SpiritImprintNodeData> offers, GameStoryFlagService flags)
        {
            var sb = new StringBuilder();
            sb.AppendLine("The spirits can deepen your imprint. Choose your next mark:");
            sb.AppendLine();

            for (int i = 0; i < offers.Count; i++)
            {
                SpiritImprintNodeData node = offers[i];
                if (node == null)
                    continue;

                string name = string.IsNullOrWhiteSpace(node.displayName) ? node.nodeId : node.displayName.Trim();
                string description = string.IsNullOrWhiteSpace(node.description) ? name : node.description.Trim();
                string formattedCost = FormatCostLong(node.unlockCost, flags);
                sb.Append(name).Append(" — ").Append(description).Append(" Cost: ").AppendLine(formattedCost);
            }

            return sb.ToString().TrimEnd();
        }

        public static bool TryExecuteUpgrade(
            BaseActor speaker,
            SpiritImprintRuntime runtime,
            string childNodeId,
            IReadOnlyList<BaseActor> partyMembers,
            GameStoryFlagService flags,
            out string failureReason)
        {
            failureReason = null;
            if (speaker == null || runtime == null || runtime.Graph == null)
            {
                failureReason = "Invalid speaker or imprint runtime.";
                return false;
            }

            if (!runtime.Graph.TryFindNode(childNodeId, out SpiritImprintNodeData node))
            {
                failureReason = $"Unknown node '{childNodeId}'.";
                return false;
            }

            string tail = GetPathTailId(runtime);
            if (node.parentNodeId != tail)
            {
                failureReason = $"'{childNodeId}' is not a valid next node.";
                return false;
            }

            List<BaseActor> ordered = OrderPartyMembersForPayment(partyMembers, speaker);
            if (!TryPayCost(node.unlockCost, ordered, flags, out failureReason))
                return false;

            if (!runtime.TryAppendChild(childNodeId, out failureReason))
            {
                RefundCost(node.unlockCost, ordered);
                return false;
            }

            Debug.Log(
                $"{LogPrefix} {speaker.DisplayName} upgraded to '{childNodeId}' via Shaman; paid {FormatCostShort(node.unlockCost)}.");
            return true;
        }

        static void RefundCost(SpiritImprintUnlockCost cost, IReadOnlyList<BaseActor> partyMembersOrdered)
        {
            if (cost.gold > 0)
                ShopGoldUtility.AddPartyGold(cost.gold);

            if (cost.items == null)
                return;

            BaseActor recipient = partyMembersOrdered != null && partyMembersOrdered.Count > 0
                ? partyMembersOrdered[0]
                : null;

            InventoryManager inventory = recipient?.GetComponent<InventoryManager>();
            if (inventory == null)
                return;

            for (int i = 0; i < cost.items.Length; i++)
            {
                SpiritImprintItemCost row = cost.items[i];
                if (row.item == null || row.quantity <= 0)
                    continue;

                inventory.AddItem(new ItemInstance(row.item, row.quantity));
            }
        }
    }
}
