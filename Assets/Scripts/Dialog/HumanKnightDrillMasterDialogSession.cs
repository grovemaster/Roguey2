using JRogue.Actors;
using JRogue.Controller.Npc;
using JRogue.Dialog;
using JRogue.Manager.Party;
using JRogue.Quest;
using JRogue.Racial;
using JRogue.Stats;
using JRogue.Stats.Racial;
using JRogue.UI.Gameplay;
using UnityEngine;

namespace JRogue.Dialog
{
    public sealed class HumanKnightDrillMasterDialogSession
    {
        const string AcceptPayload = "__accept__";
        const string TurnInPayload = "__turn_in__";
        const string CancelPayload = "__cancel__";

        readonly BaseActor _speaker;
        readonly INpcTalkTarget _target;
        readonly PortraitDefinition _portrait;
        readonly string _displayName;
        readonly string _npcId;

        public HumanKnightDrillMasterDialogSession(BaseActor speaker, INpcTalkTarget target)
        {
            GameStoryFlagService.EnsureInstance();
            QuestService.EnsureRunService();

            _speaker = speaker;
            _target = target;
            _portrait = target.Portrait;
            _displayName = target.Actor != null ? target.Actor.DisplayName : "Drill Master";
            _npcId = target.Actor != null ? target.Actor.name : string.Empty;

            NpcController npc = target.Actor as NpcController;
            if (npc != null && !string.IsNullOrWhiteSpace(npc.NpcId))
                _npcId = npc.NpcId;
        }

        public void Start()
        {
            if (!HumanKnightDrillMasterQuestLogic.IsSpeakerHuman(_speaker, out CharacterStats stats, out string rejectLine))
            {
                ShowLine(rejectLine ?? HumanKnightClassCommitService.RaceDenyMessage);
                return;
            }

            if (HumanKnightDrillMasterQuestLogic.IsAlreadyKnight(stats))
            {
                ShowLine("You already walk the Knight's path.");
                return;
            }

            if (HumanKnightDrillMasterQuestLogic.HasCommittedElsewhere(stats))
            {
                ShowLine(HumanKnightClassCommitService.ClassDenyMessage);
                return;
            }

            QuestService quests = QuestService.Instance;
            if (HumanKnightDrillMasterQuestLogic.TryGetActiveApprenticeship(_speaker, quests, out _, out _))
            {
                ShowTurnIn();
                return;
            }

            if (HumanKnightDrillMasterQuestLogic.IsApprenticeshipCompleted(_speaker, quests))
            {
                ShowLine("You already walk the Knight's path.");
                return;
            }

            ShowOffer();
        }

        void ShowOffer()
        {
            var step = new DialogChoiceStep
            {
                SpeakerName = _displayName,
                PromptText = HumanKnightDrillMasterQuestLogic.BuildOfferBodyText(),
                Portrait = _portrait,
                Options = new[]
                {
                    new DialogChoiceOptionData
                    {
                        label = "Begin drill apprenticeship",
                        payload = AcceptPayload,
                        enabled = true,
                    },
                    new DialogChoiceOptionData
                    {
                        label = "Not yet",
                        payload = CancelPayload,
                        enabled = true,
                    },
                },
            };

            NpcDialogBoxUI.EnsureInstance().ShowChoice(step, OnOfferChoice, Complete);
        }

        void OnOfferChoice(DialogChoiceOptionData option)
        {
            NpcDialogBoxUI.EnsureInstance().Close();

            if (option == null || option.payload == CancelPayload)
            {
                Complete();
                return;
            }

            if (!HumanKnightDrillMasterQuestService.TryAcceptApprenticeship(_speaker, out _, out string failureReason))
            {
                ShowLine(failureReason ?? "You cannot begin drill apprenticeship right now.");
                return;
            }

            ShowLine("Your drill apprenticeship begins. Return when you are ready to pay the initiation fee.");
        }

        void ShowTurnIn()
        {
            var step = new DialogChoiceStep
            {
                SpeakerName = _displayName,
                PromptText = HumanKnightDrillMasterQuestLogic.BuildTurnInBodyText(_speaker),
                Portrait = _portrait,
                Options = new[]
                {
                    new DialogChoiceOptionData
                    {
                        label = "Pay & commit",
                        payload = TurnInPayload,
                        enabled = HumanKnightClassCommitService.HasDrillGold(),
                    },
                    new DialogChoiceOptionData
                    {
                        label = "Not yet",
                        payload = CancelPayload,
                        enabled = true,
                    },
                },
            };

            NpcDialogBoxUI.EnsureInstance().ShowChoice(step, OnTurnInChoice, Complete);
        }

        void OnTurnInChoice(DialogChoiceOptionData option)
        {
            NpcDialogBoxUI.EnsureInstance().Close();

            if (option == null || option.payload == CancelPayload)
            {
                Complete();
                return;
            }

            if (!HumanKnightClassCommitService.HasDrillGold())
            {
                ShowLine(HumanKnightClassCommitService.GoldDenyMessage);
                return;
            }

            if (!HumanKnightDrillMasterQuestService.TryCompleteApprenticeship(_speaker, _npcId, out _, out string failureReason))
            {
                ShowLine(failureReason ?? "Initiation failed.");
                return;
            }

            ShowLine(HumanKnightDrillMasterQuestLogic.BuildCompletionLine(_speaker));
        }

        void ShowLine(string text)
        {
            var step = new DialogLineStep
            {
                SpeakerName = _displayName,
                ResolvedText = text,
                Portrait = _portrait,
            };

            NpcDialogBoxUI.EnsureInstance().ShowLine(step, FinishLine);
        }

        void FinishLine()
        {
            NpcDialogBoxUI.EnsureInstance().Close();
            Complete();
        }

        void Complete() => PartyPlayerActionCompletion.CompleteActiveMemberAction(_speaker);
    }
}
