using JRogue.Actors;
using System;
using System.Collections.Generic;
using JRogue.Quest;
using UnityEngine;

namespace JRogue.Dialog
{
    public static class DialogGraph
    {
        public const int NoNode = -1;
    }

    public enum DialogNodeKind
    {
        Line = 0,
        Choice = 1,
        Conditional = 2,
        Action = 3,
    }

    public enum DialogConditionKind
    {
        None = 0,
        StoryFlag = 1,
        NpcTalkCount = 2,
        AnyNpcTalked = 3,
        QuestState = 4,
        QuestNotStarted = 5,
        QuestReadyToTurnIn = 6,
    }

    public enum DialogActionKind
    {
        None = 0,
        OfferQuest = 1,
        CompleteQuest = 2,
    }

    [Serializable]
    public sealed class DialogLineData
    {
        [TextArea(2, 6)]
        public string textTemplate = string.Empty;
    }

    [Serializable]
    public sealed class DialogChoiceOptionData
    {
        public string label = string.Empty;
        public int responseNodeIndex = DialogGraph.NoNode;
    }

    [Serializable]
    public sealed class DialogNodeData
    {
        public DialogNodeKind kind = DialogNodeKind.Line;
        public DialogLineData line = new DialogLineData();
        public int nextNodeIndex = DialogGraph.NoNode;
        public DialogChoiceOptionData[] choices = Array.Empty<DialogChoiceOptionData>();
        public DialogConditionKind conditionKind = DialogConditionKind.None;
        public string flagId;
        public bool expectedFlagValue = true;
        public string npcIdForTalkCount;
        public int talkCountMin;
        public int talkCountMax = int.MaxValue;
        public string[] anyTalkedNpcIds = Array.Empty<string>();
        public int trueNodeIndex = DialogGraph.NoNode;
        public int falseNodeIndex = DialogGraph.NoNode;
        public string questId;
        public QuestConditionState expectedQuestState = QuestConditionState.Active;
        public DialogActionKind actionKind = DialogActionKind.None;
        public string actionQuestId;
    }

    public sealed class DialogContext
    {
        public BaseActor Speaker { get; set; }
        public BaseActor Npc { get; set; }
        public NpcDialogProfile Profile { get; set; }
        public GameStoryFlagService Flags { get; set; }
        public NpcTalkCounterService Counters { get; set; }
        public IReadOnlyDictionary<string, QuestInstance> QuestInstances { get; set; }
    }

    public enum DialogStepKind
    {
        Line,
        Choice,
        End,
    }

    public sealed class DialogLineStep
    {
        public string SpeakerName { get; set; }
        public string ResolvedText { get; set; }
        public PortraitDefinition Portrait { get; set; }
        public int NextNodeIndex { get; set; } = DialogGraph.NoNode;
    }

    public sealed class DialogChoiceStep
    {
        public string SpeakerName { get; set; }
        public string PromptText { get; set; }
        public PortraitDefinition Portrait { get; set; }
        public IReadOnlyList<DialogChoiceOptionData> Options { get; set; }
    }
}
