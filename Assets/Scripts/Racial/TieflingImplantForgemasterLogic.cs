using System.Collections.Generic;
using System.Text;
using JRogue.Actors;
using JRogue.Dialog;
using JRogue.Item;
using JRogue.Manager.Inventory;
using JRogue.Manager.Party;
using JRogue.Quest;
using JRogue.Shop;
using JRogue.Stats;
using JRogue.Stats.Racial;
using JRogue.UI.Hotbar;
using UnityEngine;

namespace JRogue.Racial
{
    public sealed class TieflingImplantInstallOffer
    {
        public CyborgImplantDefinition Implant;
        public ImplantSlot Slot;
        public bool IsReplace;
        public string ReplaceTargetName;
    }

    public sealed class TieflingImplantRemoveOffer
    {
        public ImplantSlot Slot;
        public CyborgImplantDefinition Installed;
    }

    public static class TieflingForgemasterIds
    {
        public const string NpcId = "tiefling_fleshmetal_forgemaster";
        public const string HolyLandForgemasterNpcId = "tiefling_holy_land_forgemaster";
        public const string InstallPayloadPrefix = "install:";
        public const string RemovePayloadPrefix = "remove:";
        public const string CancelPayload = "__cancel__";
    }

    public static class TieflingImplantForgemasterLogic
    {
        public const string LogPrefix = "[TieflingImplant]";

        public static bool IsSpeakerEligible(BaseActor speaker, out TieflingImplantsRuntime runtime, out string rejectLine)
        {
            runtime = null;
            rejectLine = null;

            if (speaker == null)
            {
                rejectLine = "No speaker.";
                return false;
            }

            CharacterStats stats = speaker.GetComponent<CharacterStats>();
            if (stats == null || stats.race != Race.Tiefling)
            {
                rejectLine = "This forge works fleshmetal for Tieflings only.";
                return false;
            }

            runtime = speaker.GetComponent<TieflingImplantsRuntime>();
            if (runtime == null || stats.racialSubsystem != RacialSubsystemKind.TieflingImplants)
            {
                rejectLine = "Your body cannot accept fleshmetal grafts.";
                return false;
            }

            return true;
        }

        public static List<TieflingImplantInstallOffer> BuildInstallOffers(
            TieflingImplantsRuntime runtime,
            TieflingForgemasterDefinition catalog)
        {
            var offers = new List<TieflingImplantInstallOffer>();
            if (runtime == null || catalog?.offeredImplants == null)
                return offers;

            for (int i = 0; i < catalog.offeredImplants.Count; i++)
            {
                CyborgImplantDefinition implant = catalog.offeredImplants[i];
                if (implant == null || string.IsNullOrEmpty(implant.implantId))
                    continue;

                if (!implant.TryGetTargetSlot(out ImplantSlot slot, out _))
                    continue;

                runtime.TryGetInstalled(slot, out CyborgImplantDefinition installed);
                offers.Add(new TieflingImplantInstallOffer
                {
                    Implant = implant,
                    Slot = slot,
                    IsReplace = installed != null,
                    ReplaceTargetName = installed != null ? ResolveDisplayName(installed) : null,
                });
            }

            return offers;
        }

        public static List<TieflingImplantRemoveOffer> BuildRemoveOffers(TieflingImplantsRuntime runtime)
        {
            var offers = new List<TieflingImplantRemoveOffer>();
            if (runtime == null)
                return offers;

            foreach (ImplantSlot slot in System.Enum.GetValues(typeof(ImplantSlot)))
            {
                if (!runtime.TryGetInstalled(slot, out CyborgImplantDefinition installed) || installed == null)
                    continue;

                offers.Add(new TieflingImplantRemoveOffer
                {
                    Slot = slot,
                    Installed = installed,
                });
            }

            return offers;
        }

        public static bool IsInstallChoiceEnabled(
            BaseActor speaker,
            TieflingImplantsRuntime runtime,
            TieflingImplantInstallOffer offer,
            GameStoryFlagService flags,
            out string disableReason)
        {
            disableReason = null;
            if (speaker == null || runtime == null || offer?.Implant == null)
            {
                disableReason = "invalid";
                return false;
            }

            if (runtime.HasImplantId(offer.Implant.implantId))
            {
                disableReason = "already installed";
                return false;
            }

            if (!IsInstallUnlocked(offer.Implant.installCost, flags, out disableReason))
                return false;

            List<BaseActor> ordered = OrderPartyMembersForPayment(PartyManager.Instance?.partyMembers, speaker);
            if (!CanAffordInstall(offer.Implant.installCost, ordered, flags, out disableReason))
                return false;

            return true;
        }

        public static bool IsRemoveChoiceEnabled(
            BaseActor speaker,
            TieflingImplantRemoveOffer offer,
            GameStoryFlagService flags,
            out string disableReason)
        {
            disableReason = null;
            if (speaker == null || offer?.Installed == null)
            {
                disableReason = "invalid";
                return false;
            }

            CyborgImplantRemoveCost removeCost = ResolveRemoveCost(offer.Installed);
            if (!IsRemoveUnlocked(removeCost, flags, out disableReason))
                return false;

            List<BaseActor> ordered = OrderPartyMembersForPayment(PartyManager.Instance?.partyMembers, speaker);
            return CanAffordRemove(removeCost, ordered, flags, out disableReason);
        }

        public static bool TryExecuteInstall(
            BaseActor speaker,
            TieflingImplantsRuntime runtime,
            string implantId,
            TieflingForgemasterDefinition catalog,
            IReadOnlyList<BaseActor> partyMembers,
            GameStoryFlagService flags,
            out string failureReason)
        {
            failureReason = null;
            if (!IsSpeakerEligible(speaker, out runtime, out failureReason))
                return false;

            CyborgImplantDefinition implant = FindImplantById(catalog, implantId);
            if (implant == null || !implant.TryGetTargetSlot(out ImplantSlot slot, out failureReason))
                return false;

            if (runtime.HasImplantId(implant.implantId))
            {
                failureReason = "Already installed.";
                return false;
            }

            var offer = new TieflingImplantInstallOffer
            {
                Implant = implant,
                Slot = slot,
                IsReplace = runtime.TryGetInstalled(slot, out _),
            };

            if (!IsInstallChoiceEnabled(speaker, runtime, offer, flags, out failureReason))
                return false;

            List<BaseActor> ordered = OrderPartyMembersForPayment(partyMembers, speaker);
            if (!TryPayInstallCost(implant.installCost, ordered, flags, out failureReason))
                return false;

            bool success = offer.IsReplace
                ? runtime.TryReplaceImplant(slot, implant, out failureReason)
                : runtime.TryInstallImplant(slot, implant, out failureReason);

            if (!success)
            {
                RefundInstallCost(implant.installCost, ordered);
                return false;
            }

            runtime.RefreshPassives();
            AbilityHotbarUI.EnsureInstance().RefreshAll();
            Debug.Log(
                $"{LogPrefix} {speaker.DisplayName} {(offer.IsReplace ? "replaced" : "installed")} '{implant.implantId}' at {slot} via Forgemaster; paid {FormatInstallCostShort(implant.installCost)}.");
            return true;
        }

        public static bool TryExecuteRemove(
            BaseActor speaker,
            TieflingImplantsRuntime runtime,
            ImplantSlot slot,
            IReadOnlyList<BaseActor> partyMembers,
            GameStoryFlagService flags,
            out string failureReason)
        {
            failureReason = null;
            if (!IsSpeakerEligible(speaker, out runtime, out failureReason))
                return false;

            if (!runtime.TryGetInstalled(slot, out CyborgImplantDefinition installed) || installed == null)
            {
                failureReason = "Slot is empty.";
                return false;
            }

            var offer = new TieflingImplantRemoveOffer { Slot = slot, Installed = installed };
            if (!IsRemoveChoiceEnabled(speaker, offer, flags, out failureReason))
                return false;

            CyborgImplantRemoveCost removeCost = ResolveRemoveCost(installed);
            List<BaseActor> ordered = OrderPartyMembersForPayment(partyMembers, speaker);
            if (!TryPayRemoveCost(removeCost, ordered, flags, out failureReason))
                return false;

            if (!runtime.TryRemoveImplant(slot))
            {
                RefundRemoveCost(removeCost, ordered);
                failureReason = "Could not remove implant.";
                return false;
            }

            runtime.RefreshPassives();
            AbilityHotbarUI.EnsureInstance().RefreshAll();
            Debug.Log(
                $"{LogPrefix} {speaker.DisplayName} removed '{installed.implantId}' from {slot} via Forgemaster; paid {FormatRemoveCostShort(removeCost)}.");
            return true;
        }

        public static CyborgImplantRemoveCost ResolveRemoveCost(CyborgImplantDefinition implant)
        {
            if (implant == null)
                return default;

            CyborgImplantRemoveCost authored = implant.removeCost;
            if (HasRemoveCostOverride(authored))
                return authored;

            return new CyborgImplantRemoveCost
            {
                gold = Mathf.Max(0, implant.installCost.gold / 2),
            };
        }

        static bool HasRemoveCostOverride(CyborgImplantRemoveCost cost)
        {
            if (cost.gold > 0)
                return true;

            if (cost.items != null)
            {
                for (int i = 0; i < cost.items.Length; i++)
                {
                    if (cost.items[i].item != null && cost.items[i].quantity > 0)
                        return true;
                }
            }

            if (cost.storyFlags != null)
            {
                for (int i = 0; i < cost.storyFlags.Length; i++)
                {
                    if (!string.IsNullOrWhiteSpace(cost.storyFlags[i].flagId))
                        return true;
                }
            }

            return false;
        }

        public static string BuildOfferBodyText(
            TieflingImplantsRuntime runtime,
            IReadOnlyList<TieflingImplantInstallOffer> installOffers,
            IReadOnlyList<TieflingImplantRemoveOffer> removeOffers,
            GameStoryFlagService flags)
        {
            var sb = new StringBuilder();
            sb.AppendLine("The Forgemaster opens the graft catalog. Your implants:");
            sb.AppendLine();

            bool anyInstalled = false;
            foreach (ImplantSlot slot in System.Enum.GetValues(typeof(ImplantSlot)))
            {
                if (!runtime.TryGetInstalled(slot, out CyborgImplantDefinition installed) || installed == null)
                    continue;

                anyInstalled = true;
                sb.Append("**").Append(FormatSlotDisplay(slot)).Append(":** ")
                    .AppendLine(ResolveDisplayName(installed));
            }

            if (!anyInstalled)
                sb.AppendLine("— none —");

            sb.AppendLine();
            sb.AppendLine("Available grafts:");
            sb.AppendLine();

            for (int i = 0; i < installOffers.Count; i++)
            {
                TieflingImplantInstallOffer offer = installOffers[i];
                CyborgImplantDefinition implant = offer.Implant;
                string name = ResolveDisplayName(implant);
                string description = string.IsNullOrWhiteSpace(implant.description) ? name : implant.description.Trim();
                sb.Append("**").Append(name).Append("** · **")
                    .Append(FormatSlotDisplay(offer.Slot)).Append("** — ")
                    .AppendLine(description);
                sb.Append("Install cost: ").AppendLine(FormatInstallCostLong(implant.installCost, flags));
                if (offer.IsReplace && !string.IsNullOrWhiteSpace(offer.ReplaceTargetName))
                    sb.AppendLine($"(replaces {offer.ReplaceTargetName})");
                sb.AppendLine();
            }

            for (int i = 0; i < removeOffers.Count; i++)
            {
                TieflingImplantRemoveOffer offer = removeOffers[i];
                CyborgImplantRemoveCost removeCost = ResolveRemoveCost(offer.Installed);
                sb.Append("**Remove ").Append(FormatSlotDisplay(offer.Slot)).Append("** (")
                    .Append(ResolveDisplayName(offer.Installed)).Append(") — Remove cost: ")
                    .AppendLine(FormatRemoveCostLong(removeCost, flags));
            }

            return sb.ToString().TrimEnd();
        }

        public static string FormatSlotDisplay(ImplantSlot slot) =>
            slot switch
            {
                ImplantSlot.LeftArm => "Left Arm",
                ImplantSlot.RightArm => "Right Arm",
                ImplantSlot.Torso => "Torso",
                ImplantSlot.Heart => "Heart",
                ImplantSlot.Head => "Head",
                ImplantSlot.LeftLeg => "Left Leg",
                ImplantSlot.RightLeg => "Right Leg",
                _ => slot.ToString(),
            };

        public static string ResolveDisplayName(CyborgImplantDefinition implant)
        {
            if (implant == null)
                return "Graft";

            return string.IsNullOrWhiteSpace(implant.displayName)
                ? implant.implantId
                : implant.displayName.Trim();
        }

        static CyborgImplantDefinition FindImplantById(TieflingForgemasterDefinition catalog, string implantId)
        {
            if (string.IsNullOrEmpty(implantId) || catalog?.offeredImplants == null)
                return null;

            for (int i = 0; i < catalog.offeredImplants.Count; i++)
            {
                CyborgImplantDefinition implant = catalog.offeredImplants[i];
                if (implant != null && implant.implantId == implantId)
                    return implant;
            }

            return null;
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

        public static bool IsInstallUnlocked(
            CyborgImplantInstallCost cost,
            GameStoryFlagService flags,
            out string denyReason) =>
            EvaluateFlagCosts(cost.storyFlags, flags, out denyReason);

        public static bool IsRemoveUnlocked(
            CyborgImplantRemoveCost cost,
            GameStoryFlagService flags,
            out string denyReason) =>
            EvaluateFlagCosts(cost.storyFlags, flags, out denyReason);

        public static bool CanAffordInstall(
            CyborgImplantInstallCost cost,
            IReadOnlyList<BaseActor> partyMembersOrdered,
            GameStoryFlagService flags,
            out string denyReason) =>
            CanAffordGoldAndItems(cost.gold, cost.items, partyMembersOrdered, out denyReason);

        public static bool CanAffordRemove(
            CyborgImplantRemoveCost cost,
            IReadOnlyList<BaseActor> partyMembersOrdered,
            GameStoryFlagService flags,
            out string denyReason) =>
            CanAffordGoldAndItems(cost.gold, cost.items, partyMembersOrdered, out denyReason);

        static bool CanAffordGoldAndItems(
            int gold,
            CyborgImplantItemCost[] items,
            IReadOnlyList<BaseActor> partyMembersOrdered,
            out string denyReason)
        {
            denyReason = null;

            if (gold > 0 && ShopGoldUtility.GetPartyGoldTotal() < gold)
            {
                denyReason = "insufficient funds";
                return false;
            }

            if (items != null)
            {
                for (int i = 0; i < items.Length; i++)
                {
                    CyborgImplantItemCost row = items[i];
                    if (row.item == null || row.quantity <= 0)
                        continue;

                    int owned = QuestLogic.CountItemInParty(
                        partyMembersOrdered,
                        row.item,
                        QuestActorRequirementKind.None,
                        default);

                    if (owned < row.quantity)
                    {
                        denyReason = "insufficient funds";
                        return false;
                    }
                }
            }

            return true;
        }

        static bool EvaluateFlagCosts(
            CyborgImplantFlagCost[] storyFlags,
            GameStoryFlagService flags,
            out string denyReason)
        {
            denyReason = null;
            if (storyFlags == null || flags == null)
                return true;

            for (int i = 0; i < storyFlags.Length; i++)
            {
                CyborgImplantFlagCost row = storyFlags[i];
                if (string.IsNullOrWhiteSpace(row.flagId))
                    continue;

                bool actual = flags.IsSet(row.flagId.Trim());
                if (actual != row.expectedValue)
                {
                    denyReason = "locked";
                    return false;
                }
            }

            return true;
        }

        public static bool TryPayInstallCost(
            CyborgImplantInstallCost cost,
            IReadOnlyList<BaseActor> partyMembersOrdered,
            GameStoryFlagService flags,
            out string failureReason)
        {
            if (!IsInstallUnlocked(cost, flags, out failureReason))
                return false;

            return TryPayGoldAndItems(cost.gold, cost.items, partyMembersOrdered, out failureReason);
        }

        public static bool TryPayRemoveCost(
            CyborgImplantRemoveCost cost,
            IReadOnlyList<BaseActor> partyMembersOrdered,
            GameStoryFlagService flags,
            out string failureReason)
        {
            if (!IsRemoveUnlocked(cost, flags, out failureReason))
                return false;

            return TryPayGoldAndItems(cost.gold, cost.items, partyMembersOrdered, out failureReason);
        }

        static bool TryPayGoldAndItems(
            int gold,
            CyborgImplantItemCost[] items,
            IReadOnlyList<BaseActor> partyMembersOrdered,
            out string failureReason)
        {
            failureReason = null;

            if (!CanAffordGoldAndItems(gold, items, partyMembersOrdered, out failureReason))
                return false;

            if (gold > 0 && !ShopGoldUtility.TrySpendPartyGold(gold))
            {
                failureReason = "Could not spend gold.";
                return false;
            }

            if (items != null)
            {
                for (int i = 0; i < items.Length; i++)
                {
                    CyborgImplantItemCost row = items[i];
                    if (row.item == null || row.quantity <= 0)
                        continue;

                    int removed = QuestLogic.RemoveMatchingItems(
                        partyMembersOrdered,
                        row.item,
                        row.quantity,
                        acceptGenericStacks: true);

                    if (removed < row.quantity)
                    {
                        if (gold > 0)
                            ShopGoldUtility.AddPartyGold(gold);

                        failureReason = $"Could not remove {row.quantity} × {row.item.itemName}.";
                        return false;
                    }
                }
            }

            return true;
        }

        static void RefundInstallCost(CyborgImplantInstallCost cost, IReadOnlyList<BaseActor> partyMembersOrdered)
        {
            RefundGoldAndItems(cost.gold, cost.items, partyMembersOrdered);
        }

        static void RefundRemoveCost(CyborgImplantRemoveCost cost, IReadOnlyList<BaseActor> partyMembersOrdered)
        {
            RefundGoldAndItems(cost.gold, cost.items, partyMembersOrdered);
        }

        static void RefundGoldAndItems(
            int gold,
            CyborgImplantItemCost[] items,
            IReadOnlyList<BaseActor> partyMembersOrdered)
        {
            if (gold > 0)
                ShopGoldUtility.AddPartyGold(gold);

            if (items == null)
                return;

            BaseActor recipient = partyMembersOrdered != null && partyMembersOrdered.Count > 0
                ? partyMembersOrdered[0]
                : null;

            InventoryManager inventory = recipient?.GetComponent<InventoryManager>();
            if (inventory == null)
                return;

            for (int i = 0; i < items.Length; i++)
            {
                CyborgImplantItemCost row = items[i];
                if (row.item == null || row.quantity <= 0)
                    continue;

                inventory.AddItem(new ItemInstance(row.item, row.quantity));
            }
        }

        public static bool IsInstallCostEmpty(CyborgImplantInstallCost cost) =>
            cost.gold <= 0 && IsItemCostEmpty(cost.items) && IsFlagCostEmpty(cost.storyFlags);

        public static bool IsRemoveCostEmpty(CyborgImplantRemoveCost cost) =>
            cost.gold <= 0 && IsItemCostEmpty(cost.items) && IsFlagCostEmpty(cost.storyFlags);

        static bool IsItemCostEmpty(CyborgImplantItemCost[] items)
        {
            if (items == null)
                return true;

            for (int i = 0; i < items.Length; i++)
            {
                if (items[i].item != null && items[i].quantity > 0)
                    return false;
            }

            return true;
        }

        static bool IsFlagCostEmpty(CyborgImplantFlagCost[] storyFlags)
        {
            if (storyFlags == null)
                return true;

            for (int i = 0; i < storyFlags.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(storyFlags[i].flagId))
                    return false;
            }

            return true;
        }

        public static string FormatInstallCostLong(CyborgImplantInstallCost cost, GameStoryFlagService flags)
        {
            if (IsInstallCostEmpty(cost))
                return "Free";

            var parts = new List<string>();
            if (cost.gold > 0)
                parts.Add($"{cost.gold} gold");

            AppendItemCostParts(cost.items, parts);
            AppendFlagCostParts(cost.storyFlags, parts);
            return parts.Count == 0 ? "Free" : string.Join(", ", parts);
        }

        public static string FormatInstallCostShort(CyborgImplantInstallCost cost)
        {
            if (IsInstallCostEmpty(cost))
                return "Free";

            var parts = new List<string>();
            if (cost.gold > 0)
                parts.Add($"{cost.gold} gold");

            AppendItemCostParts(cost.items, parts);
            AppendFlagCostParts(cost.storyFlags, parts);
            return parts.Count == 0 ? "Free" : string.Join(", ", parts);
        }

        public static string FormatRemoveCostLong(CyborgImplantRemoveCost cost, GameStoryFlagService flags)
        {
            if (IsRemoveCostEmpty(cost))
                return "Free";

            var parts = new List<string>();
            if (cost.gold > 0)
                parts.Add($"{cost.gold} gold");

            AppendItemCostParts(cost.items, parts);
            AppendFlagCostParts(cost.storyFlags, parts);
            return parts.Count == 0 ? "Free" : string.Join(", ", parts);
        }

        public static string FormatRemoveCostShort(CyborgImplantRemoveCost cost)
        {
            if (IsRemoveCostEmpty(cost))
                return "Free";

            var parts = new List<string>();
            if (cost.gold > 0)
                parts.Add($"{cost.gold} gold");

            AppendItemCostParts(cost.items, parts);
            AppendFlagCostParts(cost.storyFlags, parts);
            return parts.Count == 0 ? "Free" : string.Join(", ", parts);
        }

        static void AppendItemCostParts(CyborgImplantItemCost[] items, List<string> parts)
        {
            if (items == null)
                return;

            for (int i = 0; i < items.Length; i++)
            {
                CyborgImplantItemCost row = items[i];
                if (row.item == null || row.quantity <= 0)
                    continue;

                parts.Add($"{row.item.itemName} ×{row.quantity}");
            }
        }

        static void AppendFlagCostParts(CyborgImplantFlagCost[] storyFlags, List<string> parts)
        {
            if (storyFlags == null)
                return;

            for (int i = 0; i < storyFlags.Length; i++)
            {
                CyborgImplantFlagCost row = storyFlags[i];
                if (string.IsNullOrWhiteSpace(row.flagId))
                    continue;

                parts.Add($"flag: {row.flagId.Trim()}");
            }
        }
    }
}
