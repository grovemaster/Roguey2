using JRogue.Actors;
using JRogue.Controller.Npc;
using JRogue.Quest;
using JRogue.UI.Gameplay;
using UnityEngine;

namespace JRogue.Dialog
{
    public sealed class NpcDialogSession
    {
        readonly DialogContext _context;
        readonly PortraitDefinition _portrait;
        readonly INpcTalkTarget _target;
        bool _talkCountIncremented;

        public NpcDialogSession(BaseActor speaker, INpcTalkTarget target)
        {
            GameStoryFlagService.EnsureInstance();
            NpcTalkCounterService.EnsureInstance();
            QuestService.EnsureRunService();

            _target = target;
            _context = new DialogContext
            {
                Speaker = speaker,
                Npc = target.Actor,
                Profile = target.DialogProfile,
                Flags = GameStoryFlagService.Instance,
                Counters = NpcTalkCounterService.Instance,
                QuestInstances = QuestService.Instance?.ActiveInstances,
            };
            _portrait = target.Portrait;
        }

        public void Start()
        {
            if (_context.Profile == null)
                return;

            QuestService.Instance?.NotifyNpcTalked(ResolveNpcId(), _context.Speaker);

            int entryIndex = DialogGraphEvaluator.ResolveEntryNodeIndex(_context.Profile, _context);
            if (entryIndex < 0)
                return;

            PresentNode(entryIndex);
        }

        void PresentNode(int nodeIndex)
        {
            if (nodeIndex < 0)
            {
                Complete();
                return;
            }

            NpcDialogProfile profile = _context.Profile;
            nodeIndex = DialogGraphEvaluator.ResolveNodeIndex(profile, nodeIndex, _context);
            if (nodeIndex < 0)
            {
                Complete();
                return;
            }

            DialogNodeData node = DialogGraphEvaluator.GetNode(profile, nodeIndex);
            if (node == null)
            {
                Complete();
                return;
            }

            if (node.kind == DialogNodeKind.Action)
            {
                ExecuteAction(node);
                PresentNode(node.nextNodeIndex);
                return;
            }

            if (node.kind == DialogNodeKind.Choice)
            {
                DialogChoiceStep choice = DialogGraphEvaluator.BuildChoiceStep(profile, nodeIndex, _context, _portrait);
                if (choice == null)
                {
                    Complete();
                    return;
                }

                MaybeIncrementTalkCount();
                NpcDialogBoxUI.EnsureInstance().ShowChoice(choice, OnChoiceSelected);
                return;
            }

            DialogLineStep line = DialogGraphEvaluator.BuildLineStep(profile, nodeIndex, _context, _portrait);
            if (line == null)
            {
                Complete();
                return;
            }

            MaybeIncrementTalkCount();
            int nextIndex = line.NextNodeIndex;
            NpcDialogBoxUI.EnsureInstance().ShowLine(line, () => PresentNode(nextIndex));
        }

        void ExecuteAction(DialogNodeData node)
        {
            QuestService quests = QuestService.Instance;
            if (quests == null || node.actionKind == DialogActionKind.None)
                return;

            string questId = string.IsNullOrWhiteSpace(node.actionQuestId) ? node.questId : node.actionQuestId;
            if (string.IsNullOrWhiteSpace(questId))
                return;

            switch (node.actionKind)
            {
                case DialogActionKind.OfferQuest:
                    if (quests.TryAccept(questId, out string acceptDeny))
                        Debug.Log($"{QuestLogic.LogPrefix} accepted via dialog.");
                    else
                        Debug.Log($"{QuestLogic.LogPrefix} accept failed: {acceptDeny}");
                    break;

                case DialogActionKind.CompleteQuest:
                    if (quests.TryTurnIn(questId, ResolveNpcId(), out string turnInDeny))
                        Debug.Log($"{QuestLogic.LogPrefix} turned in via dialog.");
                    else
                        Debug.Log($"{QuestLogic.LogPrefix} turn-in failed: {turnInDeny}");
                    break;
            }
        }

        void OnChoiceSelected(DialogChoiceOptionData option)
        {
            if (option == null || option.responseNodeIndex < 0)
            {
                Complete();
                return;
            }

            PresentNode(option.responseNodeIndex);
        }

        void MaybeIncrementTalkCount()
        {
            if (_talkCountIncremented || _context.Profile == null || !_context.Profile.incrementTalkCountOnStart)
                return;

            _talkCountIncremented = true;
            _context.Counters?.Increment(_context.Profile.npcId);
        }

        string ResolveNpcId()
        {
            if (_target is NpcController npc)
                return npc.NpcId;

            if (!string.IsNullOrWhiteSpace(_context.Profile?.npcId))
                return _context.Profile.npcId.Trim();

            return _context.Npc != null ? _context.Npc.name : string.Empty;
        }

        void Complete()
        {
            if (_context.Profile != null && !string.IsNullOrWhiteSpace(_context.Profile.completionFlagId))
                _context.Flags?.Set(_context.Profile.completionFlagId);

            NpcDialogBoxUI.EnsureInstance().Close();
        }
    }
}
