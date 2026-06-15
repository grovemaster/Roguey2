using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Dialog;
using JRogue.Manager.Party;
using JRogue.Racial;
using JRogue.UI.Gameplay;
using UnityEngine;

namespace JRogue.Racial
{
    public static class DwarfAncestorAltarService
    {
        public static bool TryBeginPayRespects(BaseActor speaker, DwarfClanDefinition clan)
        {
            if (clan == null)
            {
                ShowFeedback("Hall of Ancestors", "This altar has no clan binding.");
                return false;
            }

            if (!DwarfAncestorLearnLogic.CanBeginAltarCeremony(out string denyReason))
            {
                ShowFeedback(ResolveAltarName(clan), denyReason);
                return false;
            }

            if (!DwarfAncestorLearnLogic.IsSpeakerEligibleForAltar(
                    speaker,
                    clan,
                    out _,
                    out _,
                    out string rejectLine))
            {
                ShowFeedback(ResolveAltarName(clan), rejectLine);
                return false;
            }

            List<DwarfAncestorFrontierOffer> offers = DwarfAncestorLearnLogic.GetFrontierOffers(speaker, clan);
            if (offers.Count == 0)
            {
                ShowFeedback(ResolveAltarName(clan), DwarfAncestorLearnLogic.CompleteMessage);
                return true;
            }

            ShowLearnChoice(speaker, clan, offers);
            return true;
        }

        static void ShowLearnChoice(
            BaseActor speaker,
            DwarfClanDefinition clan,
            IReadOnlyList<DwarfAncestorFrontierOffer> offers)
        {
            var options = new List<DialogChoiceOptionData>(offers.Count);
            for (int i = 0; i < offers.Count; i++)
            {
                DwarfAncestorFrontierOffer offer = offers[i];
                if (offer?.Node == null || string.IsNullOrWhiteSpace(offer.Node.nodeId))
                    continue;

                string title = DwarfAncestorLearnLogic.ResolveNodeTitle(offer.Node);
                string label = offer.Selectable ? title : $"{title} — {offer.DisabledReason}";
                options.Add(new DialogChoiceOptionData
                {
                    label = label,
                    payload = offer.Node.nodeId,
                    enabled = offer.Selectable,
                });
            }

            var step = new DialogChoiceStep
            {
                SpeakerName = ResolveAltarName(clan),
                PromptText = DwarfAncestorLearnLogic.BuildOfferBodyText(clan, offers),
                Portrait = null,
                Options = options,
            };

            NpcDialogBoxUI.EnsureInstance().ShowChoice(
                step,
                option => OnLearnChoice(speaker, clan, option),
                () => PartyPlayerActionCompletion.CompleteActiveMemberAction(speaker));
        }

        static void OnLearnChoice(BaseActor speaker, DwarfClanDefinition clan, DialogChoiceOptionData option)
        {
            NpcDialogBoxUI.EnsureInstance().Close();

            if (option == null || string.IsNullOrWhiteSpace(option.payload) || !option.enabled)
            {
                PartyPlayerActionCompletion.CompleteActiveMemberAction(speaker);
                return;
            }

            if (!DwarfAncestorLearnService.TryLearnNode(speaker, clan, option.payload.Trim(), out string failureReason))
            {
                ShowFeedback(ResolveAltarName(clan), failureReason ?? "The ancestors withhold this technique.");
                return;
            }

            ShowFeedback(ResolveAltarName(clan), "The ancestors accept your offering. The mark is set.");
        }

        static string ResolveAltarName(DwarfClanDefinition clan)
        {
            if (clan == null)
                return "Hall of Ancestors";

            if (!string.IsNullOrWhiteSpace(clan.shortName))
                return $"Hall of Ancestors — {clan.shortName.Trim()}";

            return "Hall of Ancestors";
        }

        static void ShowFeedback(string speakerName, string text)
        {
            var step = new DialogLineStep
            {
                SpeakerName = speakerName,
                ResolvedText = text,
                Portrait = null,
            };

            NpcDialogBoxUI.EnsureInstance().ShowLine(
                step,
                () =>
                {
                    NpcDialogBoxUI.EnsureInstance().Close();
                    PartyPlayerActionCompletion.CompleteActiveMemberAction(PartyManager.Instance?.GetActiveMember());
                });
        }
    }
}
