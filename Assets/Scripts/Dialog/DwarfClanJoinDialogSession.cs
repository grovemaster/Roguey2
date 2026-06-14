using JRogue.Actors;
using JRogue.Controller.Npc;
using JRogue.Dialog;
using JRogue.Manager.Party;
using JRogue.Racial;
using JRogue.UI.Gameplay;
using UnityEngine;

namespace JRogue.Dialog
{
    public sealed class DwarfClanJoinDialogSession
    {
        const string AcceptPayload = "__accept__";
        const string DeclinePayload = "__decline__";

        readonly BaseActor _speaker;
        readonly INpcTalkTarget _target;
        readonly PortraitDefinition _portrait;
        readonly string _displayName;
        readonly DwarfClanDefinition _clan;

        public DwarfClanJoinDialogSession(BaseActor speaker, INpcTalkTarget target, DwarfClanDefinition clan)
        {
            _speaker = speaker;
            _target = target;
            _portrait = target.Portrait;
            _displayName = target.Actor != null ? target.Actor.DisplayName : "Clan Steward";
            _clan = clan;
        }

        public void Start()
        {
            if (!DwarfClanJoinLogic.IsSpeakerDwarf(_speaker, out _, out string rejectLine))
            {
                ShowLine(rejectLine ?? DwarfClanJoinLogic.RaceDenyMessage);
                return;
            }

            DwarfClanMembershipRuntime membership = _speaker.GetComponent<DwarfClanMembershipRuntime>();
            if (membership != null && membership.IsAffiliated)
            {
                if (membership.MatchesClan(_clan))
                    ShowLine("You already walk this clan's Ancestor path. Pay respects at the Hall altar to learn more.");
                else
                    ShowLine(DwarfClanJoinLogic.WrongClanMessage);
                return;
            }

            if (!DwarfClanJoinLogic.CanBeginJoinCeremony(out string denyReason))
            {
                ShowLine(denyReason ?? "You cannot swear allegiance here.");
                return;
            }

            ShowOffer();
        }

        void ShowOffer()
        {
            var step = new DialogChoiceStep
            {
                SpeakerName = _displayName,
                PromptText = DwarfClanJoinLogic.BuildOfferBodyText(_clan),
                Portrait = _portrait,
                Options = new[]
                {
                    new DialogChoiceOptionData
                    {
                        label = "Swear allegiance",
                        payload = AcceptPayload,
                        enabled = true,
                    },
                    new DialogChoiceOptionData
                    {
                        label = "Not yet",
                        payload = DeclinePayload,
                        enabled = true,
                    },
                },
            };

            NpcDialogBoxUI.EnsureInstance().ShowChoice(step, OnOfferChoice, Complete);
        }

        void OnOfferChoice(DialogChoiceOptionData option)
        {
            NpcDialogBoxUI.EnsureInstance().Close();

            if (option == null || option.payload == DeclinePayload)
            {
                Complete();
                return;
            }

            if (!DwarfClanJoinService.TryJoinClan(_speaker, _clan, out string failureReason))
            {
                ShowLine(failureReason ?? "You cannot join this clan right now.");
                return;
            }

            ShowLine(DwarfClanJoinLogic.BuildSuccessLine(_clan));
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
