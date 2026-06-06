using System;
using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Dialog;
using JRogue.Item;
using JRogue.Manager.Equipment;
using JRogue.Manager.Inventory;
using JRogue.Manager.Party;
using UnityEngine;

namespace JRogue.Quest
{
    public static class QuestLogic
    {
        public const string LogPrefix = "[Quest]";

        public static bool ActorMatchesRequirement(
            QuestActorRequirement requirement,
            BaseActor actor,
            PartyManager party)
        {
            switch (requirement.kind)
            {
                case QuestActorRequirementKind.None:
                    return true;
                case QuestActorRequirementKind.PartyMemberId:
                {
                    if (actor == null || string.IsNullOrWhiteSpace(requirement.partyMemberId))
                        return false;

                    return string.Equals(
                        PartyMemberId.GetMemberId(actor),
                        requirement.partyMemberId.Trim(),
                        StringComparison.OrdinalIgnoreCase);
                }
                case QuestActorRequirementKind.ActiveLeader:
                    return actor != null && party != null && party.GetActiveMember() == actor;
                default:
                    return true;
            }
        }

        public static QuestObjectiveProgress[] CreateInitialProgress(QuestDefinition definition)
        {
            if (definition?.objectives == null || definition.objectives.Length == 0)
                return Array.Empty<QuestObjectiveProgress>();

            var progress = new QuestObjectiveProgress[definition.objectives.Length];
            for (int i = 0; i < definition.objectives.Length; i++)
            {
                QuestObjectiveDefinition objective = definition.objectives[i];
                progress[i] = new QuestObjectiveProgress
                {
                    objectiveId = ResolveObjectiveId(objective, i),
                    current = 0,
                    required = ResolveRequiredCount(objective),
                    completed = false,
                };
            }

            return progress;
        }

        public static string ResolveObjectiveId(QuestObjectiveDefinition objective, int index)
        {
            if (!string.IsNullOrWhiteSpace(objective.objectiveId))
                return objective.objectiveId.Trim();

            return $"objective_{index}";
        }

        public static int ResolveRequiredCount(QuestObjectiveDefinition objective)
        {
            switch (objective.kind)
            {
                case QuestObjectiveKind.KillSpecies:
                    return Mathf.Max(1, objective.killCount);
                case QuestObjectiveKind.CollectItem:
                case QuestObjectiveKind.DeliverItem:
                    return Mathf.Max(1, objective.itemQuantity);
                default:
                    return 1;
            }
        }

        public static bool EvaluatePrerequisites(
            QuestDefinition definition,
            IReadOnlyDictionary<string, QuestInstance> instances,
            GameStoryFlagService flags,
            IReadOnlyList<BaseActor> partyMembers,
            out string denyReason)
        {
            denyReason = null;
            if (definition == null)
            {
                denyReason = "Quest definition is missing.";
                return false;
            }

            if (definition.acceptPrerequisites == null || definition.acceptPrerequisites.Length == 0)
                return true;

            for (int i = 0; i < definition.acceptPrerequisites.Length; i++)
            {
                QuestPrerequisite prerequisite = definition.acceptPrerequisites[i];
                if (EvaluatePrerequisite(prerequisite, instances, flags, partyMembers, out denyReason))
                    continue;

                return false;
            }

            denyReason = null;
            return true;
        }

        public static bool EvaluatePrerequisite(
            QuestPrerequisite prerequisite,
            IReadOnlyDictionary<string, QuestInstance> instances,
            GameStoryFlagService flags,
            IReadOnlyList<BaseActor> partyMembers,
            out string denyReason)
        {
            denyReason = null;
            switch (prerequisite.kind)
            {
                case QuestPrerequisiteKind.StoryFlag:
                {
                    bool actual = flags != null && flags.IsSet(prerequisite.flagId);
                    if (actual == prerequisite.expectedFlagValue)
                        return true;

                    denyReason = prerequisite.expectedFlagValue
                        ? $"Requires story flag '{prerequisite.flagId}'."
                        : $"Story flag '{prerequisite.flagId}' must not be set.";
                    return false;
                }
                case QuestPrerequisiteKind.QuestCompleted:
                {
                    string questId = prerequisite.questId?.Trim();
                    if (string.IsNullOrEmpty(questId))
                    {
                        denyReason = "Quest prerequisite id is empty.";
                        return false;
                    }

                    if (instances != null
                        && instances.TryGetValue(questId, out QuestInstance instance)
                        && instance.state == QuestRuntimeState.Completed)
                    {
                        return true;
                    }

                    denyReason = $"Requires completed quest '{questId}'.";
                    return false;
                }
                case QuestPrerequisiteKind.QuestNotStarted:
                {
                    string questId = prerequisite.questId?.Trim();
                    if (string.IsNullOrEmpty(questId))
                    {
                        denyReason = "Quest prerequisite id is empty.";
                        return false;
                    }

                    if (instances == null || !instances.TryGetValue(questId, out QuestInstance instance))
                        return true;

                    denyReason = $"Quest '{questId}' already started.";
                    return false;
                }
                case QuestPrerequisiteKind.HasItem:
                {
                    int required = Mathf.Max(1, prerequisite.itemQuantity);
                    int owned = CountItemInParty(
                        partyMembers,
                        prerequisite.item,
                        QuestActorRequirementKind.None,
                        default);
                    if (owned >= required)
                        return true;

                    denyReason = prerequisite.item != null
                        ? $"Requires {required} × {prerequisite.item.itemName}."
                        : "Required item is missing.";
                    return false;
                }
                default:
                    return true;
            }
        }

        public static bool HasActiveOrCompletedQuest(
            IReadOnlyDictionary<string, QuestInstance> instances,
            string questId)
        {
            if (instances == null || string.IsNullOrWhiteSpace(questId))
                return false;

            return instances.TryGetValue(questId.Trim(), out QuestInstance instance)
                   && instance.state != QuestRuntimeState.Failed;
        }

        public static QuestConditionState ResolveConditionState(
            IReadOnlyDictionary<string, QuestInstance> instances,
            string questId)
        {
            if (instances == null || string.IsNullOrWhiteSpace(questId))
                return QuestConditionState.NotStarted;

            if (!instances.TryGetValue(questId.Trim(), out QuestInstance instance))
                return QuestConditionState.NotStarted;

            switch (instance.state)
            {
                case QuestRuntimeState.Active:
                    return QuestConditionState.Active;
                case QuestRuntimeState.ReadyToTurnIn:
                    return QuestConditionState.ReadyToTurnIn;
                case QuestRuntimeState.Completed:
                    return QuestConditionState.Completed;
                case QuestRuntimeState.Failed:
                    return QuestConditionState.Failed;
                default:
                    return QuestConditionState.NotStarted;
            }
        }

        public static bool EvaluateDialogQuestCondition(
            DialogConditionKind kind,
            string questId,
            QuestConditionState expectedState,
            IReadOnlyDictionary<string, QuestInstance> instances)
        {
            QuestConditionState actual = ResolveConditionState(instances, questId);
            switch (kind)
            {
                case DialogConditionKind.QuestState:
                    return actual == expectedState;
                case DialogConditionKind.QuestNotStarted:
                    return actual == QuestConditionState.NotStarted;
                case DialogConditionKind.QuestReadyToTurnIn:
                    return actual == QuestConditionState.ReadyToTurnIn;
                default:
                    return false;
            }
        }

        public static bool AreRequiredObjectivesComplete(
            QuestDefinition definition,
            QuestObjectiveProgress[] progress)
        {
            if (definition?.objectives == null || definition.objectives.Length == 0)
                return true;

            for (int i = 0; i < definition.objectives.Length; i++)
            {
                QuestObjectiveDefinition objective = definition.objectives[i];
                if (objective.optional)
                    continue;

                QuestObjectiveProgress entry = FindProgress(progress, ResolveObjectiveId(objective, i));
                if (!entry.completed)
                    return false;
            }

            return true;
        }

        public static QuestRuntimeState ResolveRuntimeState(
            QuestDefinition definition,
            QuestObjectiveProgress[] progress,
            QuestRuntimeState current)
        {
            if (current == QuestRuntimeState.Completed || current == QuestRuntimeState.Failed)
                return current;

            if (!AreRequiredObjectivesComplete(definition, progress))
                return QuestRuntimeState.Active;

            if (definition.autoCompleteOnObjectives)
                return QuestRuntimeState.ReadyToTurnIn;

            return QuestRuntimeState.ReadyToTurnIn;
        }

        public static void RefreshScanObjectives(
            QuestDefinition definition,
            QuestInstance instance,
            GameStoryFlagService flags,
            IReadOnlyList<BaseActor> partyMembers,
            PartyManager party)
        {
            if (definition?.objectives == null || instance?.progress == null)
                return;

            for (int i = 0; i < definition.objectives.Length; i++)
            {
                QuestObjectiveDefinition objective = definition.objectives[i];
                string objectiveId = ResolveObjectiveId(objective, i);
                int progressIndex = FindProgressIndex(instance.progress, objectiveId);
                if (progressIndex < 0)
                    continue;

                QuestObjectiveProgress entry = instance.progress[progressIndex];
                if (entry.completed)
                    continue;

                switch (objective.kind)
                {
                    case QuestObjectiveKind.StoryFlag:
                        if (flags != null && flags.IsSet(objective.flagId))
                            LatchObjective(ref entry, 1);
                        break;
                    case QuestObjectiveKind.CollectItem:
                    case QuestObjectiveKind.DeliverItem:
                    {
                        int owned = CountItemInParty(
                            partyMembers,
                            objective.item,
                            objective.actorRequirement.kind,
                            objective.actorRequirement,
                            party);
                        entry.current = Mathf.Min(owned, entry.required);
                        if (entry.current >= entry.required)
                            LatchObjective(ref entry, entry.required);
                        break;
                    }
                    case QuestObjectiveKind.EquipItem:
                    {
                        if (IsItemEquippedByMatchingMember(
                                partyMembers,
                                objective.item,
                                objective.equipSlot,
                                objective.actorRequirement,
                                party))
                        {
                            LatchObjective(ref entry, 1);
                        }

                        break;
                    }
                }

                instance.progress[progressIndex] = entry;
            }
        }

        public static void NotifyEnemyKilled(
            QuestDefinition definition,
            QuestInstance instance,
            string speciesId,
            BaseActor killer,
            PartyManager party)
        {
            if (definition?.objectives == null || instance?.progress == null)
                return;

            for (int i = 0; i < definition.objectives.Length; i++)
            {
                QuestObjectiveDefinition objective = definition.objectives[i];
                if (objective.kind != QuestObjectiveKind.KillSpecies)
                    continue;

                if (!string.Equals(objective.speciesId, speciesId, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!ActorMatchesRequirement(objective.actorRequirement, killer, party))
                    continue;

                string objectiveId = ResolveObjectiveId(objective, i);
                int progressIndex = FindProgressIndex(instance.progress, objectiveId);
                if (progressIndex < 0)
                    continue;

                QuestObjectiveProgress entry = instance.progress[progressIndex];
                if (entry.completed)
                    continue;

                entry.current = Mathf.Min(entry.current + 1, entry.required);
                if (entry.current >= entry.required)
                    LatchObjective(ref entry, entry.required);

                instance.progress[progressIndex] = entry;
            }
        }

        public static void NotifyNpcTalked(
            QuestDefinition definition,
            QuestInstance instance,
            string npcId,
            BaseActor speaker,
            PartyManager party)
        {
            if (definition?.objectives == null || instance?.progress == null)
                return;

            for (int i = 0; i < definition.objectives.Length; i++)
            {
                QuestObjectiveDefinition objective = definition.objectives[i];
                if (objective.kind != QuestObjectiveKind.TalkToNpc)
                    continue;

                if (!string.Equals(objective.npcId, npcId, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!ActorMatchesRequirement(objective.actorRequirement, speaker, party))
                    continue;

                string objectiveId = ResolveObjectiveId(objective, i);
                int progressIndex = FindProgressIndex(instance.progress, objectiveId);
                if (progressIndex < 0)
                    continue;

                QuestObjectiveProgress entry = instance.progress[progressIndex];
                if (entry.completed)
                    continue;

                LatchObjective(ref entry, 1);
                instance.progress[progressIndex] = entry;
            }
        }

        public static void NotifyItemEquipped(
            QuestDefinition definition,
            QuestInstance instance,
            BaseActor actor,
            ItemInstance item,
            EquipmentSlot slot,
            PartyManager party)
        {
            if (definition?.objectives == null || instance?.progress == null || item?.Definition == null)
                return;

            for (int i = 0; i < definition.objectives.Length; i++)
            {
                QuestObjectiveDefinition objective = definition.objectives[i];
                if (objective.kind != QuestObjectiveKind.EquipItem)
                    continue;

                if (objective.item != null && objective.item != item.Definition)
                    continue;

                if (objective.equipSlot != slot)
                    continue;

                if (!ActorMatchesRequirement(objective.actorRequirement, actor, party))
                    continue;

                string objectiveId = ResolveObjectiveId(objective, i);
                int progressIndex = FindProgressIndex(instance.progress, objectiveId);
                if (progressIndex < 0)
                    continue;

                QuestObjectiveProgress entry = instance.progress[progressIndex];
                if (entry.completed)
                    continue;

                LatchObjective(ref entry, 1);
                instance.progress[progressIndex] = entry;
            }
        }

        public static int CountItemInParty(
            IReadOnlyList<BaseActor> partyMembers,
            ItemData item,
            QuestActorRequirementKind scopeKind,
            QuestActorRequirement requirement,
            PartyManager party = null)
        {
            if (item == null || partyMembers == null)
                return 0;

            int total = 0;
            for (int i = 0; i < partyMembers.Count; i++)
            {
                BaseActor member = partyMembers[i];
                if (member == null)
                    continue;

                if (scopeKind != QuestActorRequirementKind.None
                    && !ActorMatchesRequirement(requirement, member, party))
                {
                    continue;
                }

                InventoryManager inventory = member.GetComponent<InventoryManager>();
                if (inventory == null)
                    continue;

                foreach (ItemInstance carried in inventory.CarriedItems)
                {
                    if (carried?.Definition == item)
                        total += carried.Quantity;
                }
            }

            return total;
        }

        public static bool CanReceiveRewardItems(
            QuestRewardBundle rewards,
            BaseActor recipient,
            out string denyReason)
        {
            denyReason = null;
            if (rewards.items == null || rewards.items.Length == 0)
                return true;

            InventoryManager inventory = recipient?.GetComponent<InventoryManager>();
            if (inventory == null)
            {
                denyReason = "No inventory available for quest rewards.";
                return false;
            }

            for (int i = 0; i < rewards.items.Length; i++)
            {
                ItemData item = rewards.items[i];
                if (item == null)
                    continue;

                int quantity = ResolveRewardQuantity(rewards.itemQuantities, i);
                if (quantity <= 0)
                    continue;

                var probe = new ItemInstance(item, quantity);
                if (!inventory.CanCarry(probe))
                {
                    denyReason = $"Not enough room for {item.itemName}.";
                    return false;
                }
            }

            return true;
        }

        public static int ResolveRewardQuantity(int[] quantities, int index)
        {
            if (quantities != null && index >= 0 && index < quantities.Length && quantities[index] > 0)
                return quantities[index];

            return 1;
        }

        public static bool TryRemoveDeliverItems(
            QuestDefinition definition,
            IReadOnlyList<BaseActor> partyMembers,
            out string denyReason)
        {
            denyReason = null;
            if (definition?.objectives == null)
                return true;

            for (int i = 0; i < definition.objectives.Length; i++)
            {
                QuestObjectiveDefinition objective = definition.objectives[i];
                if (objective.kind != QuestObjectiveKind.DeliverItem)
                    continue;

                int required = ResolveRequiredCount(objective);
                int removed = RemoveMatchingItems(
                    partyMembers,
                    objective.item,
                    required,
                    objective.acceptGenericStacks);

                if (removed < required)
                {
                    denyReason = objective.item != null
                        ? $"Need {required} × {objective.item.itemName} to turn in."
                        : "Missing required delivery items.";
                    return false;
                }
            }

            return true;
        }

        public static int RemoveMatchingItems(
            IReadOnlyList<BaseActor> partyMembers,
            ItemData item,
            int quantity,
            bool acceptGenericStacks)
        {
            if (item == null || partyMembers == null || quantity <= 0)
                return 0;

            int remaining = quantity;
            for (int memberIndex = 0; memberIndex < partyMembers.Count && remaining > 0; memberIndex++)
            {
                BaseActor member = partyMembers[memberIndex];
                InventoryManager inventory = member?.GetComponent<InventoryManager>();
                if (inventory == null)
                    continue;

                for (int itemIndex = inventory.CarriedItems.Count - 1;
                     itemIndex >= 0 && remaining > 0;
                     itemIndex--)
                {
                    ItemInstance carried = inventory.CarriedItems[itemIndex];
                    if (carried?.Definition != item)
                        continue;

                    if (!acceptGenericStacks && carried.Definition.category != ItemCategory.QuestItem)
                        continue;

                    int take = Mathf.Min(remaining, carried.Quantity);
                    if (take >= carried.Quantity)
                    {
                        inventory.TryRemoveCarriedAt(itemIndex);
                    }
                    else
                    {
                        carried.Quantity -= take;
                    }

                    remaining -= take;
                }
            }

            return quantity - remaining;
        }

        public static QuestObjectiveProgress FindProgress(QuestObjectiveProgress[] progress, string objectiveId)
        {
            int index = FindProgressIndex(progress, objectiveId);
            return index >= 0 ? progress[index] : default;
        }

        public static int FindProgressIndex(QuestObjectiveProgress[] progress, string objectiveId)
        {
            if (progress == null || string.IsNullOrWhiteSpace(objectiveId))
                return -1;

            for (int i = 0; i < progress.Length; i++)
            {
                if (string.Equals(progress[i].objectiveId, objectiveId.Trim(), StringComparison.Ordinal))
                    return i;
            }

            return -1;
        }

        public static string FormatJournalObjectiveLine(
            QuestObjectiveDefinition objective,
            QuestObjectiveProgress progress,
            int index)
        {
            string label = string.IsNullOrWhiteSpace(objective.journalText)
                ? objective.kind.ToString()
                : objective.journalText;

            if (objective.actorRequirement.kind == QuestActorRequirementKind.PartyMemberId
                && !string.IsNullOrWhiteSpace(objective.actorRequirement.partyMemberId)
                && !label.Contains(":"))
            {
                label = $"{objective.actorRequirement.partyMemberId}: {label}";
            }

            if (progress.completed)
                return $"☑ {label}";

            if (objective.kind == QuestObjectiveKind.KillSpecies
                || objective.kind == QuestObjectiveKind.CollectItem)
            {
                return $"☐ {label} ({progress.current}/{progress.required})";
            }

            return $"☐ {label} ({progress.current}/{progress.required})";
        }

        static bool IsItemEquippedByMatchingMember(
            IReadOnlyList<BaseActor> partyMembers,
            ItemData item,
            EquipmentSlot slot,
            QuestActorRequirement requirement,
            PartyManager party)
        {
            if (item == null || partyMembers == null)
                return false;

            for (int i = 0; i < partyMembers.Count; i++)
            {
                BaseActor member = partyMembers[i];
                if (!ActorMatchesRequirement(requirement, member, party))
                    continue;

                EquipmentManager equipment = member.GetComponent<EquipmentManager>();
                if (equipment == null)
                    continue;

                ItemInstance equipped = equipment.GetEquippedInstance(slot);
                if (equipped?.Definition == item)
                    return true;
            }

            return false;
        }

        static void LatchObjective(ref QuestObjectiveProgress entry, int value)
        {
            entry.current = value;
            entry.completed = true;
        }
    }
}
