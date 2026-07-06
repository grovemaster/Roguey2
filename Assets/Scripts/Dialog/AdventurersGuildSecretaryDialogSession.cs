using System.Collections.Generic;
using System.Globalization;
using JRogue.Actors;
using JRogue.Controller.Npc;
using JRogue.Dialog;
using JRogue.Manager.Party;
using JRogue.Organizations;
using JRogue.Party.Recruitment;
using JRogue.UI.Gameplay;
using UnityEngine;

namespace JRogue.Dialog
{
    public sealed class AdventurersGuildSecretaryDialogSession
    {
        const string RankUpPayload = "__rank_up__";
        const string RecruitPayload = "__recruit__";
        const string LeavePayload = "__leave__";
        const string PromoteAgainPayload = "__promote_again__";
        const string PromoteSomeoneElsePayload = "__promote_someone_else__";
        const string DonePayload = "__done__";
        const string BackPayload = "__back__";
        const string RecruitAgainPayload = "__recruit_again__";
        const string ConfirmYesPayload = "__confirm_yes__";
        const string ConfirmNoPayload = "__confirm_no__";
        const string MemberPayloadPrefix = "__member__";
        const string RecruitPayloadPrefix = "__recruit_id__";

        readonly BaseActor _speaker;
        readonly INpcTalkTarget _target;
        readonly PortraitDefinition _portrait;
        readonly string _displayName;
        readonly OrganizationDefinition _organization;
        readonly PartyManager _party;

        BaseActor _lastPromotedMember;
        string _pendingRecruitId;

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

            string recruitHint = BuildRecruitMenuHint();
            if (!string.IsNullOrEmpty(recruitHint))
                prompt += recruitHint;

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
                    label = "Recruit party member",
                    payload = RecruitPayload,
                    enabled = CanEnableRecruitMenu(),
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

        string BuildRecruitMenuHint()
        {
            PartyCapacityService capacity = PartyCapacityService.Instance;
            if (capacity != null && !capacity.CanAddMember(_party))
            {
                int living = capacity.GetLivingMemberCount(_party);
                return $"\n\nYour party is full ({living}/{capacity.MaxPartyMembers}).";
            }

            if (!PartyRecruitmentService.HasAvailableRecruitsOnBoard())
                return "\n\nNo adventurers are seeking a party right now.";

            return string.Empty;
        }

        bool CanEnableRecruitMenu()
        {
            PartyCapacityService capacity = PartyCapacityService.Instance;
            if (capacity != null && !capacity.CanAddMember(_party))
                return false;

            return PartyRecruitmentService.HasAvailableRecruitsOnBoard();
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
            {
                ShowMemberPicker();
                return;
            }

            if (option.payload == RecruitPayload)
                ShowRecruitPicker();
        }

        void ShowRecruitPicker()
        {
            List<PartyRecruitOptionView> options = PartyRecruitmentService.GetRecruitOptions(
                _organization,
                _party);

            if (options.Count == 0)
            {
                ShowLine("No adventurers are seeking a party right now.", ShowMainMenu);
                return;
            }

            var choiceOptions = new List<DialogChoiceOptionData>();
            for (int i = 0; i < options.Count; i++)
            {
                PartyRecruitOptionView view = options[i];
                PartyRecruitDefinition recruit = view.Recruit;
                string label = $"{recruit.displayName}  (rank {recruit.guildRank}, {view.GoldCost} gold)";
                if (!view.CanSelect && !string.IsNullOrEmpty(view.DenyReason))
                    label += $" — {view.DenyReason}";

                choiceOptions.Add(new DialogChoiceOptionData
                {
                    label = label,
                    payload = RecruitPayloadPrefix + recruit.recruitId,
                    enabled = view.CanSelect,
                });
            }

            choiceOptions.Add(new DialogChoiceOptionData
            {
                label = "Back",
                payload = BackPayload,
                enabled = true,
            });

            ShowChoice("Who would you like to recruit?", choiceOptions, OnRecruitChoice);
        }

        void OnRecruitChoice(DialogChoiceOptionData option)
        {
            NpcDialogBoxUI.EnsureInstance().Close();

            if (option == null || option.payload == BackPayload)
            {
                ShowMainMenu();
                return;
            }

            if (option.payload == null || !option.payload.StartsWith(RecruitPayloadPrefix, System.StringComparison.Ordinal))
                return;

            _pendingRecruitId = option.payload.Substring(RecruitPayloadPrefix.Length);
            ShowRecruitConfirm();
        }

        void ShowRecruitConfirm()
        {
            PartyRecruitCatalog catalog = PartyRecruitCatalog.LoadDefault();
            PartyRecruitDefinition recruit = catalog?.FindById(_pendingRecruitId);
            if (recruit == null)
            {
                ShowLine("That adventurer is no longer available.", ShowRecruitPicker);
                return;
            }

            int cost = PartyRecruitmentService.GetRecruitCost(recruit);
            var options = new List<DialogChoiceOptionData>
            {
                new()
                {
                    label = "Yes",
                    payload = ConfirmYesPayload,
                    enabled = true,
                },
                new()
                {
                    label = "No",
                    payload = ConfirmNoPayload,
                    enabled = true,
                },
            };

            ShowChoice(
                $"Recruit {recruit.displayName} for {cost} gold?",
                options,
                OnRecruitConfirmChoice);
        }

        void OnRecruitConfirmChoice(DialogChoiceOptionData option)
        {
            NpcDialogBoxUI.EnsureInstance().Close();

            if (option == null || option.payload == ConfirmNoPayload)
            {
                ShowRecruitPicker();
                return;
            }

            if (option.payload != ConfirmYesPayload)
                return;

            if (!PartyRecruitmentService.TryRecruit(
                    _organization,
                    _party,
                    _pendingRecruitId,
                    out string message))
            {
                ShowLine(message, ShowRecruitPicker);
                return;
            }

            ShowLine(message, ShowPostRecruitMenu);
        }

        void ShowPostRecruitMenu()
        {
            bool canRecruitAgain = PartyRecruitmentService.CanOpenRecruitMenu(_party)
                && HasSelectableRecruit();

            var options = new List<DialogChoiceOptionData>();
            if (canRecruitAgain)
            {
                options.Add(new DialogChoiceOptionData
                {
                    label = "Recruit another",
                    payload = RecruitAgainPayload,
                    enabled = true,
                });
            }

            options.Add(new DialogChoiceOptionData
            {
                label = "Done",
                payload = DonePayload,
                enabled = true,
            });

            ShowChoice("Anything else?", options, OnPostRecruitChoice);
        }

        bool HasSelectableRecruit()
        {
            List<PartyRecruitOptionView> options = PartyRecruitmentService.GetRecruitOptions(
                _organization,
                _party);

            for (int i = 0; i < options.Count; i++)
            {
                if (options[i].CanSelect)
                    return true;
            }

            return false;
        }

        void OnPostRecruitChoice(DialogChoiceOptionData option)
        {
            NpcDialogBoxUI.EnsureInstance().Close();

            if (option == null || option.payload == DonePayload)
            {
                ShowMainMenu();
                return;
            }

            if (option.payload == RecruitAgainPayload)
                ShowRecruitPicker();
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
                payload = BackPayload,
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

            if (option == null || option.payload == BackPayload)
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
