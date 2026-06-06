using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Controller.Npc;
using JRogue.Dialog;
using JRogue.Manager.Party;
using JRogue.UI.Gameplay;
using UnityEngine;

namespace JRogue.Racial
{
    public static class SpiritImprintShamanIds
    {
        public const string NpcId = "shaman_barbarian";
    }

    public sealed class SpiritImprintShamanDialogSession
    {
        const string CancelPayload = "__cancel__";

        readonly BaseActor _speaker;
        readonly INpcTalkTarget _target;
        readonly PortraitDefinition _portrait;
        readonly string _displayName;

        public SpiritImprintShamanDialogSession(BaseActor speaker, INpcTalkTarget target)
        {
            GameStoryFlagService.EnsureInstance();
            _speaker = speaker;
            _target = target;
            _portrait = target.Portrait;
            _displayName = target.Actor != null ? target.Actor.DisplayName : "Shaman Barbarian";
        }

        public void Start()
        {
            if (!SpiritImprintUpgradeLogic.IsSpeakerEligible(_speaker, out SpiritImprintRuntime runtime, out string rejectLine))
            {
                ShowLine(rejectLine ?? "Hello. You are not a Barbarian.");
                return;
            }

            IReadOnlyList<SpiritImprintNodeData> offers = SpiritImprintUpgradeLogic.GetNextNodeOffers(runtime);
            if (offers == null || offers.Count == 0)
            {
                ShowLine("You have all the upgrades.");
                return;
            }

            ShowOffer(offers);
        }

        void ShowOffer(IReadOnlyList<SpiritImprintNodeData> offers)
        {
            GameStoryFlagService flags = GameStoryFlagService.Instance;
            var options = new List<DialogChoiceOptionData>(offers.Count + 1);

            for (int i = 0; i < offers.Count; i++)
            {
                SpiritImprintNodeData node = offers[i];
                if (node == null || string.IsNullOrWhiteSpace(node.nodeId))
                    continue;

                string displayName = string.IsNullOrWhiteSpace(node.displayName) ? node.nodeId : node.displayName.Trim();
                string shortCost = SpiritImprintUpgradeLogic.FormatCostShort(node.unlockCost);
                bool affordable = SpiritImprintUpgradeService.CanAffordNode(_speaker, node);
                options.Add(new DialogChoiceOptionData
                {
                    label = $"{displayName}, {shortCost}",
                    payload = node.nodeId,
                    enabled = affordable,
                });
            }

            options.Add(new DialogChoiceOptionData
            {
                label = "Cancel",
                payload = CancelPayload,
                enabled = true,
            });

            var step = new DialogChoiceStep
            {
                SpeakerName = _displayName,
                PromptText = SpiritImprintUpgradeLogic.BuildOfferBodyText(offers, flags),
                Portrait = _portrait,
                Options = options,
            };

            NpcDialogBoxUI.EnsureInstance().ShowChoice(step, OnChoiceSelected, Complete);
        }

        void OnChoiceSelected(DialogChoiceOptionData option)
        {
            NpcDialogBoxUI.EnsureInstance().Close();

            if (option == null || option.payload == CancelPayload || string.IsNullOrWhiteSpace(option.payload))
            {
                Complete();
                return;
            }

            if (!SpiritImprintUpgradeService.TryExecuteUpgrade(_speaker, option.payload.Trim(), out _))
            {
                ShowLine("You no longer have what the spirits require.");
                return;
            }

            ShowLine("The mark is set.");
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

        void Complete()
        {
            PartyPlayerActionCompletion.CompleteActiveMemberAction(_speaker);
        }
    }
}
