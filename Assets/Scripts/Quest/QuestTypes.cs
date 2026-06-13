using System;
using JRogue.Item;
using UnityEngine;

namespace JRogue.Quest
{
    public enum QuestOwnership : byte
    {
        PartyShared = 0,
        PerPartyMember = 1,
    }

    public enum QuestRuntimeState
    {
        Active = 0,
        ReadyToTurnIn = 1,
        Completed = 2,
        Failed = 3,
    }

    public enum QuestActorRequirementKind
    {
        None = 0,
        PartyMemberId = 1,
        ActiveLeader = 2,
    }

    public enum QuestObjectiveKind
    {
        KillSpecies = 0,
        StoryFlag = 1,
        TalkToNpc = 2,
        EquipItem = 3,
        CollectItem = 4,
        DeliverItem = 5,
        EnterZone = 6,
    }

    public enum QuestPrerequisiteKind
    {
        StoryFlag = 0,
        QuestCompleted = 1,
        QuestNotStarted = 2,
        HasItem = 3,
    }

    public enum QuestConditionState
    {
        NotStarted = 0,
        Active = 1,
        ReadyToTurnIn = 2,
        Completed = 3,
        Failed = 4,
    }

    [Serializable]
    public struct QuestActorRequirement
    {
        public QuestActorRequirementKind kind;
        public string partyMemberId;
    }

    [Serializable]
    public struct QuestPrerequisite
    {
        public QuestPrerequisiteKind kind;
        public string flagId;
        public bool expectedFlagValue;
        public string questId;
        public ItemData item;
        [Min(1)] public int itemQuantity;
    }

    [Serializable]
    public struct QuestObjectiveDefinition
    {
        public string objectiveId;
        [TextArea(1, 3)] public string journalText;
        public bool optional;
        public QuestActorRequirement actorRequirement;
        public QuestObjectiveKind kind;
        public string speciesId;
        [Min(1)] public int killCount;
        public string flagId;
        public string npcId;
        public ItemData item;
        [Min(1)] public int itemQuantity;
        public EquipmentSlot equipSlot;
        public bool acceptGenericStacks;
        public string zoneId;
    }

    [Serializable]
    public struct QuestRewardBundle
    {
        [Min(0)] public int gold;
        public ItemData[] items;
        public int[] itemQuantities;
        public string[] setFlagsOnComplete;
        [Min(0)] public int partyExperience;
    }

    [Serializable]
    public struct QuestObjectiveProgress
    {
        public string objectiveId;
        public int current;
        public int required;
        public bool completed;
    }

    [Serializable]
    public sealed class QuestInstance
    {
        public string questId;
        public string ownerPartyMemberId;
        public QuestRuntimeState state;
        public QuestObjectiveProgress[] progress = Array.Empty<QuestObjectiveProgress>();
        public int acceptOrder;
        public bool isNew;
        public bool isPinned;
        public long pinSequence;
        public string failReason;
    }
}
