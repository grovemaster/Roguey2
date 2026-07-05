using System.Collections.Generic;
using System.Globalization;
using JRogue.Actors;
using JRogue.Controller.Npc;
using JRogue.Dialog;
using JRogue.Manager.Party;
using JRogue.Organizations;
using JRogue.UI.Gameplay;
using UnityEngine;

namespace JRogue.Dialog
{
    public sealed class AdventurersGuildSecretaryDialogSession
    {
        const string RankUpPayload = "__rank_up__";
        const string LeavePayload = "__leave__";
        const string PromoteAgainPayload = "__promote_again__";
        const string PromoteSomeoneElsePayload = "__promote_someone_else__";
        const string DonePayload = "__done__";
        const string MemberPayloadPrefix = "__member__";

        readonly BaseActor _speaker;
        readonly INpcTalkTarget _target;
        readonly PortraitDefinition _portrait;
        readonly string _displayName;
        readonly OrganizationDefinition _organization;
        readonly PartyManager _party;

        BaseActor _lastPromotedMember;

        public AdventurersGuildSecretaryDialogSession(
            BaseActor speaker,
            INpcTalkTarget target,
            OrganizationDefinition organization,
            PartyManager party)
        {
            _speaker = speaker;
            _target = target;
            _portrait = target.Portrait;
            _displayName = target.Actor != null ? target.Actor.DisplayName : "Guild Secretary";
            _organization = organization;
            _party = party;
        }

        public void Start()
        {
            if (_organization == null)
            {
                ShowLine("The guild registry is unavailable.");
                return;
            }

            ShowGreeting();
        }

        void ShowGreeting()
        {
            int partyRank = OrganizationRankService.GetPartyRank(_organization, _party);
            string partyName = _speaker != null ? _speaker.DisplayName : "adventurer";
            string greeting =
                $"Welcome to the Adventurer's Guild, {partyName}. " +
                $"Your party holds guild rank {partyRank}.\n\n" +
                "How may I assist you?";

            ShowLine(greeting, ShowMainMenu);
        }

        void ShowMainMenu()
        {
            List<BaseActor> eligible = OrganizationRankService.GetEligibleRankUpMembers(_organization, _party);
            bool anyEligible = eligible.Count > 0;

            string prompt = anyEligible
                ? "Select a service."
                : "Select a service.\n\nNo one meets the essence requirements to rank up yet.";

            var options = new List<DialogChoiceOptionData>
            {
                new()
                {
                    label = "Rank up",
                    payload = RankUpPayload,
                    enabled = anyEligible,
                },
                new()
                {
                    label = "Leave",
                    payload = LeavePayload,
                    enabled = true,
                },
            };

            ShowChoice(prompt, options, OnMainMenuChoice);
        }

        void OnMainMenuChoice(DialogChoiceOptionData option)
        {
            NpcDialogBoxUI.EnsureInstance().Close();

            if (option == null || option.payload == LeavePayload)
            {
                Complete();
                return;
            }

            if (option.payload == RankUpPayload)
                ShowMemberPicker();
        }

        void ShowMemberPicker()
        {
            List<BaseActor> eligible = OrganizationRankService.GetEligibleRankUpMembers(_organization, _party);
            if (eligible.Count == 0)
            {
                ShowLine("No party member currently qualifies for promotion.", ShowMainMenu);
                return;
            }

            var options = new List<DialogChoiceOptionData>();
            for (int i = 0; i < eligible.Count; i++)
                options.Add(BuildMemberOption(eligible[i]));

            options.Add(new DialogChoiceOptionData
            {
                label = "Back",
                payload = DonePayload,
                enabled = true,
            });

            ShowChoice("Who will register for promotion?", options, OnMemberChoice);
        }

        DialogChoiceOptionData BuildMemberOption(BaseActor member)
        {
            OrganizationRankService.TryGetRank(member, _organization, out int currentRank);
            OrganizationRankService.CanRankUp(_organization, member, out int targetRank, out _);
            int points = OrganizationRankService.GetScore(_organization, member);
            int threshold = _organization.GetThresholdForRank(targetRank);

            return new DialogChoiceOptionData
            {
                label = $"{member.DisplayName}  (rank {currentRank} → {targetRank}, {points}/{threshold} EP)",
                payload = MemberPayloadPrefix + EntityId.ToULong(member.gameObject.GetEntityId())
                    .ToString(CultureInfo.InvariantCulture),
                enabled = true,
            };
        }

        void OnMemberChoice(DialogChoiceOptionData option)
        {
            NpcDialogBoxUI.EnsureInstance().Close();

            if (option == null || option.payload == DonePayload)
            {
                ShowMainMenu();
                return;
            }

            if (!TryResolveMember(option.payload, out BaseActor member))
            {
                ShowLine("That adventurer is no longer available.", ShowMemberPicker);
                return;
            }

            PromoteMember(member);
        }

        void PromoteMember(BaseActor member)
        {
            if (!OrganizationRankService.TryRankUp(_organization, member))
            {
                ShowLine($"{member.DisplayName} does not meet the requirements right now.", ShowMemberPicker);
                return;
            }

            _lastPromotedMember = member;
            OrganizationRankService.TryGetRank(member, _organization, out int newRank);
            ShowLine(
                $"{member.DisplayName} is now registered at guild rank {newRank}.",
                ShowPostPromotionMenu);
        }

        void ShowPostPromotionMenu()
        {
            BaseActor member = _lastPromotedMember;
            bool canPromoteAgain = member != null
                && OrganizationRankService.CanRankUp(_organization, member, out _, out _);

            var options = new List<DialogChoiceOptionData>();
            if (canPromoteAgain)
            {
                options.Add(new DialogChoiceOptionData
                {
                    label = "Promote again",
                    payload = PromoteAgainPayload,
                    enabled = true,
                });
            }

            options.Add(new DialogChoiceOptionData
            {
                label = "Promote someone else",
                payload = PromoteSomeoneElsePayload,
                enabled = true,
            });
            options.Add(new DialogChoiceOptionData
            {
                label = "Done",
                payload = DonePayload,
                enabled = true,
            });

            ShowChoice("Anything else?", options, OnPostPromotionChoice);
        }

        void OnPostPromotionChoice(DialogChoiceOptionData option)
        {
            NpcDialogBoxUI.EnsureInstance().Close();

            if (option == null || option.payload == DonePayload)
            {
                Complete();
                return;
            }

            if (option.payload == PromoteAgainPayload && _lastPromotedMember != null)
            {
                PromoteMember(_lastPromotedMember);
                return;
            }

            ShowMemberPicker();
        }

        bool TryResolveMember(string payload, out BaseActor member)
        {
            member = null;
            if (string.IsNullOrEmpty(payload) || !payload.StartsWith(MemberPayloadPrefix, System.StringComparison.Ordinal))
                return false;

            if (!ulong.TryParse(
                    payload.Substring(MemberPayloadPrefix.Length),
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out ulong entityRaw))
                return false;

            EntityId targetId = EntityId.FromULong(entityRaw);

            if (_party?.partyMembers == null)
                return false;

            for (int i = 0; i < _party.partyMembers.Count; i++)
            {
                BaseActor candidate = _party.partyMembers[i];
                if (candidate != null && candidate.gameObject.GetEntityId().Equals(targetId))
                {
                    member = candidate;
                    return true;
                }
            }

            return false;
        }

        void ShowLine(string text, System.Action onAdvance = null)
        {
            var step = new DialogLineStep
            {
                SpeakerName = _displayName,
                ResolvedText = text,
                Portrait = _portrait,
            };

            NpcDialogBoxUI.EnsureInstance().ShowLine(step, () =>
            {
                if (onAdvance != null)
                    onAdvance();
                else
                    Complete();
            });
        }

        void ShowChoice(string prompt, IReadOnlyList<DialogChoiceOptionData> options, System.Action<DialogChoiceOptionData> onChoice)
        {
            var step = new DialogChoiceStep
            {
                SpeakerName = _displayName,
                PromptText = prompt,
                Portrait = _portrait,
                Options = options,
            };

            NpcDialogBoxUI.EnsureInstance().ShowChoice(step, onChoice, Complete);
        }

        void Complete()
        {
            NpcDialogBoxUI.EnsureInstance().Close();

            if (_speaker == null)
                return;

            PartyPlayerActionCompletion.CompleteActiveMemberAction(_speaker);
        }
    }
}
