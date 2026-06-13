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
    public sealed class HumanMageTutorDialogSession
    {
        const string AcceptPayload = "__accept__";
        const string TurnInPayload = "__turn_in__";
        const string CancelPayload = "__cancel__";

        readonly BaseActor _speaker;
        readonly INpcTalkTarget _target;
        readonly PortraitDefinition _portrait;
        readonly string _displayName;
        readonly string _npcId;

        public HumanMageTutorDialogSession(BaseActor speaker, INpcTalkTarget target)
        {
            GameStoryFlagService.EnsureInstance();
            QuestService.EnsureRunService();

            _speaker = speaker;
            _target = target;
            _portrait = target.Portrait;
            _displayName = target.Actor != null ? target.Actor.DisplayName : "Mage Tutor";
            _npcId = target.Actor != null ? target.Actor.name : string.Empty;

            NpcController npc = target.Actor as NpcController;
            if (npc != null && !string.IsNullOrWhiteSpace(npc.NpcId))
                _npcId = npc.NpcId;
        }

        public void Start()
        {
            if (!HumanMageTutorQuestLogic.IsSpeakerHuman(_speaker, out CharacterStats stats, out string rejectLine))
            {
                ShowLine(rejectLine ?? HumanMageClassCommitService.RaceDenyMessage);
                return;
            }

            if (HumanMageTutorQuestLogic.IsAlreadyMage(stats))
            {
                ShowLine("You already walk the arcane path.");
                return;
            }

            if (HumanMageTutorQuestLogic.HasCommittedElsewhere(stats))
            {
                ShowLine(HumanMageClassCommitService.ClassDenyMessage);
                return;
            }

            if (!HumanMageClassCommitService.CanBeginMageTraining(_speaker, out string essenceLine)
                && !string.IsNullOrEmpty(essenceLine)
                && essenceLine != HumanMageClassCommitService.RaceDenyMessage
                && essenceLine != HumanMageClassCommitService.ClassDenyMessage)
            {
                ShowLine(
                    "Relinquish your consumed essences before I can teach you. A mage cannot consume essences.");
                return;
            }

            QuestService quests = QuestService.Instance;
            if (HumanMageTutorQuestLogic.TryGetActiveApprenticeship(_speaker, quests, out _, out _))
            {
                ShowTurnIn();
                return;
            }

            if (HumanMageTutorQuestLogic.IsApprenticeshipCompleted(_speaker, quests))
            {
                ShowLine("You already walk the arcane path.");
                return;
            }

            ShowOffer();
        }

        void ShowOffer()
        {
            var step = new DialogChoiceStep
            {
                SpeakerName = _displayName,
                PromptText = HumanMageTutorQuestLogic.BuildOfferBodyText(),
                Portrait = _portrait,
                Options = new[]
                {
                    new DialogChoiceOptionData
                    {
                        label = "Begin apprenticeship",
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

            if (!HumanMageTutorQuestService.TryAcceptApprenticeship(_speaker, out _, out string failureReason))
            {
                ShowLine(failureReason ?? "You cannot begin apprenticeship right now.");
                return;
            }

            ShowLine("Your apprenticeship begins. Return when you are ready to pay the initiation fee.");
        }

        void ShowTurnIn()
        {
            var step = new DialogChoiceStep
            {
                SpeakerName = _displayName,
                PromptText = HumanMageTutorQuestLogic.BuildTurnInBodyText(_speaker),
                Portrait = _portrait,
                Options = new[]
                {
                    new DialogChoiceOptionData
                    {
                        label = "Pay & commit",
                        payload = TurnInPayload,
                        enabled = HumanMageClassCommitService.HasApprenticeshipGold(),
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

            if (!HumanMageClassCommitService.HasApprenticeshipGold())
            {
                ShowLine(HumanMageClassCommitService.GoldDenyMessage);
                return;
            }

            if (!HumanMageTutorQuestService.TryCompleteApprenticeship(_speaker, _npcId, out _, out string failureReason))
            {
                ShowLine(failureReason ?? "Initiation failed.");
                return;
            }

            ShowLine(HumanMageTutorQuestLogic.BuildCompletionLine(_speaker));
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
