using JRogue.Actors;
using JRogue.Controller.Npc;
using JRogue.Manager.Party;
using JRogue.Quest;
using JRogue.Racial;
using JRogue.UI.Gameplay;
using UnityEngine;

namespace JRogue.Dialog
{
    public sealed class DragonianElderDialogSession
    {
        const string AcceptPayload = "__accept__";
        const string TurnInPayload = "__turn_in__";
        const string CancelPayload = "__cancel__";

        readonly BaseActor _speaker;
        readonly INpcTalkTarget _target;
        readonly DragonianElderDefinition _elder;
        readonly PortraitDefinition _portrait;
        readonly string _displayName;
        readonly string _npcId;

        public DragonianElderDialogSession(
            BaseActor speaker,
            INpcTalkTarget target,
            DragonianElderDefinition elder)
        {
            GameStoryFlagService.EnsureInstance();
            QuestService.EnsureRunService();

            _speaker = speaker;
            _target = target;
            _elder = elder;
            _portrait = target.Portrait;
            _displayName = !string.IsNullOrWhiteSpace(elder?.displayName)
                ? elder.displayName
                : target.Actor != null ? target.Actor.DisplayName : "Dragonian Elder";
            _npcId = !string.IsNullOrWhiteSpace(elder?.npcId)
                ? elder.npcId
                : target.Actor != null ? target.Actor.name : string.Empty;
        }

        public void Start()
        {
            if (_elder == null)
            {
                ShowLine("The elder is unavailable.");
                return;
            }

            if (!DragonianElderQuestLogic.IsSpeakerEligible(_speaker, out _, out string rejectLine))
            {
                ShowLine(rejectLine ?? "This elder teaches draconic word-forms to Dragonians only.");
                return;
            }

            QuestService quests = QuestService.Instance;
            if (DragonianElderQuestLogic.TryGetReadyTurnInQuest(_elder, _speaker, quests, out QuestDefinition readyQuest, out _))
            {
                ShowTurnIn(readyQuest);
                return;
            }

            string memberId = QuestLogic.ResolveMemberId(_speaker);
            if (DragonianElderQuestLogic.HasActiveQuestInChain(_elder, quests?.ActiveInstances, memberId))
            {
                ShowLine("Finish your current lesson with me before the next.");
                return;
            }

            if (DragonianElderQuestLogic.IsChainCompleteForMember(_elder, quests?.ActiveInstances, memberId))
            {
                ShowLine("You have learned all word-forms I can teach.");
                return;
            }

            if (!DragonianElderQuestLogic.TryResolveNextOffer(_elder, _speaker, quests, out QuestDefinition nextQuest, out string denyReason))
            {
                ShowLine(denyReason ?? "The elder has nothing to teach you yet.");
                return;
            }

            ShowOffer(nextQuest);
        }

        void ShowOffer(QuestDefinition quest)
        {
            var step = new DialogChoiceStep
            {
                SpeakerName = _displayName,
                PromptText = DragonianElderQuestLogic.BuildOfferBodyText(quest),
                Portrait = _portrait,
                Options = new[]
                {
                    new DialogChoiceOptionData
                    {
                        label = "Accept",
                        payload = AcceptPayload,
                        enabled = true,
                    },
                    new DialogChoiceOptionData
                    {
                        label = "Not now",
                        payload = CancelPayload,
                        enabled = true,
                    },
                },
            };

            NpcDialogBoxUI.EnsureInstance().ShowChoice(step, option => OnOfferChoice(option, quest), Complete);
        }

        void OnOfferChoice(DialogChoiceOptionData option, QuestDefinition quest)
        {
            NpcDialogBoxUI.EnsureInstance().Close();

            if (option == null || option.payload == CancelPayload)
            {
                Complete();
                return;
            }

            if (!DragonianElderQuestService.TryAcceptNextQuest(_elder, _speaker, out QuestDefinition accepted, out string failureReason))
            {
                ShowLine(failureReason ?? "You cannot accept that lesson right now.");
                return;
            }

            ShowLine($"The trial begins: {accepted.displayTitle}.");
        }

        void ShowTurnIn(QuestDefinition quest)
        {
            var step = new DialogChoiceStep
            {
                SpeakerName = _displayName,
                PromptText = DragonianElderQuestLogic.BuildTurnInBodyText(quest, _speaker),
                Portrait = _portrait,
                Options = new[]
                {
                    new DialogChoiceOptionData
                    {
                        label = "Complete quest",
                        payload = TurnInPayload,
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

            NpcDialogBoxUI.EnsureInstance().ShowChoice(step, option => OnTurnInChoice(option, quest), Complete);
        }

        void OnTurnInChoice(DialogChoiceOptionData option, QuestDefinition quest)
        {
            NpcDialogBoxUI.EnsureInstance().Close();

            if (option == null || option.payload == CancelPayload)
            {
                Complete();
                return;
            }

            if (!DragonianElderQuestService.TryTurnInReadyQuest(
                    _elder,
                    _speaker,
                    _npcId,
                    out QuestDefinition completed,
                    out string failureReason))
            {
                ShowLine(failureReason ?? "The lesson could not be sealed.");
                return;
            }

            ShowLine(DragonianElderQuestLogic.BuildSuccessLine(completed ?? quest, _speaker));
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
