using JRogue.Actors;
using JRogue.Controller.Npc;
using JRogue.Dialog;
using JRogue.Manager.Party;
using JRogue.Quest;
using JRogue.Racial;
using JRogue.UI.Gameplay;
using UnityEngine;

namespace JRogue.Dialog
{
    public sealed class DwarfClanStewardDialogSession
    {
        const string AcceptJoinPayload = "__accept_join__";
        const string DeclinePayload = "__decline__";
        const string DonatePayload = "__donate__";
        const string AcceptQuestPayload = "__accept_quest__";
        const string TurnInQuestPayload = "__turn_in_quest__";
        const string DonateSmallPayload = "__donate_10__";
        const string DonateMediumPayload = "__donate_25__";
        const string DonateLargePayload = "__donate_50__";

        readonly BaseActor _speaker;
        readonly INpcTalkTarget _target;
        readonly PortraitDefinition _portrait;
        readonly string _displayName;
        readonly string _npcId;
        readonly DwarfClanDefinition _clan;

        public DwarfClanStewardDialogSession(BaseActor speaker, INpcTalkTarget target, DwarfClanDefinition clan)
        {
            QuestService.EnsureRunService();
            DwarfClanWorldState.EnsureInstance();

            _speaker = speaker;
            _target = target;
            _portrait = target.Portrait;
            _displayName = target.Actor != null ? target.Actor.DisplayName : "Clan Steward";
            _npcId = target.Actor != null ? target.Actor.name : string.Empty;
            if (target.Actor is NpcController npc && !string.IsNullOrWhiteSpace(npc.NpcId))
                _npcId = npc.NpcId;
            _clan = clan;
        }

        public void Start()
        {
            if (_clan == null)
            {
                ShowLine("This steward serves no clan.");
                return;
            }

            if (!DwarfClanJoinLogic.IsSpeakerDwarf(_speaker, out _, out string rejectLine))
            {
                ShowLine(rejectLine ?? DwarfClanJoinLogic.RaceDenyMessage);
                return;
            }

            DwarfClanMembershipRuntime membership = _speaker.GetComponent<DwarfClanMembershipRuntime>();
            if (membership == null || !membership.IsAffiliated)
            {
                ShowJoinOffer();
                return;
            }

            if (!membership.MatchesClan(_clan))
            {
                ShowLine(DwarfClanJoinLogic.WrongClanMessage);
                return;
            }

            ShowMemberMenu();
        }

        void ShowJoinOffer()
        {
            if (!DwarfClanJoinLogic.CanBeginJoinCeremony(out string denyReason))
            {
                ShowLine(denyReason ?? "You cannot swear allegiance here.");
                return;
            }

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
                        payload = AcceptJoinPayload,
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

            NpcDialogBoxUI.EnsureInstance().ShowChoice(step, OnJoinChoice, Complete);
        }

        void OnJoinChoice(DialogChoiceOptionData option)
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

        void ShowMemberMenu()
        {
            string questId = DwarfClanQuestLogic.ResolveDevotionQuestId(_clan);
            QuestService quests = QuestService.Instance;
            int prestige = DwarfClanWorldState.EnsureInstance().GetPrestige(_clan.clanId);
            if (prestige <= 0)
                prestige = _clan.startingPrestige;

            var options = new System.Collections.Generic.List<DialogChoiceOptionData>
            {
                new()
                {
                    label = "Offer gold to the treasury",
                    payload = DonatePayload,
                    enabled = true,
                },
            };

            if (!DwarfClanQuestLogic.IsDevotionQuestCompleted(quests, questId)
                && !DwarfClanQuestLogic.TryGetActiveDevotionQuest(quests, questId, out _))
            {
                options.Add(new DialogChoiceOptionData
                {
                    label = "Take a clan devotion errand",
                    payload = AcceptQuestPayload,
                    enabled = true,
                });
            }

            if (DwarfClanQuestLogic.IsReadyToTurnIn(quests, questId))
            {
                options.Add(new DialogChoiceOptionData
                {
                    label = "Report devotion errand complete",
                    payload = TurnInQuestPayload,
                    enabled = true,
                });
            }

            options.Add(new DialogChoiceOptionData
            {
                label = "Leave",
                payload = DeclinePayload,
                enabled = true,
            });

            var step = new DialogChoiceStep
            {
                SpeakerName = _displayName,
                PromptText =
                    $"You walk the {_clan.shortName} path. Clan prestige: {prestige}.\n\n"
                    + "Raise prestige through devotion errands and treasury offerings, "
                    + "then learn deeper techniques at the Hall altar.",
                Portrait = _portrait,
                Options = options,
            };

            NpcDialogBoxUI.EnsureInstance().ShowChoice(step, OnMemberMenuChoice, Complete);
        }

        void OnMemberMenuChoice(DialogChoiceOptionData option)
        {
            NpcDialogBoxUI.EnsureInstance().Close();

            if (option == null || option.payload == DeclinePayload)
            {
                Complete();
                return;
            }

            if (option.payload == DonatePayload)
            {
                ShowDonationMenu();
                return;
            }

            if (option.payload == AcceptQuestPayload)
            {
                if (!DwarfClanQuestService.TryAcceptDevotionQuest(_speaker, _clan, out _, out string error))
                {
                    ShowLine(error ?? "You cannot take that errand now.");
                    return;
                }

                ShowLine(
                    "The steward entrusts you with a simple devotion errand. "
                    + "Report back when you are ready to seal it.");
                return;
            }

            if (option.payload == TurnInQuestPayload)
            {
                if (!DwarfClanQuestService.TryTurnInDevotionQuest(
                        _speaker,
                        _clan,
                        _npcId,
                        out QuestDefinition completed,
                        out string error))
                {
                    ShowLine(error ?? "You cannot report that errand yet.");
                    return;
                }

                int total = DwarfClanWorldState.Instance.GetPrestige(_clan.clanId);
                ShowLine(
                    $"The clan records your service (+{completed.rewards.clanPrestige} prestige). "
                    + $"Clan prestige is now {total}.");
            }
        }

        void ShowDonationMenu()
        {
            int prestige = DwarfClanWorldState.EnsureInstance().GetPrestige(_clan.clanId);
            if (prestige <= 0)
                prestige = _clan.startingPrestige;

            var step = new DialogChoiceStep
            {
                SpeakerName = _displayName,
                PromptText = DwarfClanDonationLogic.BuildDonationPrompt(_clan, prestige),
                Portrait = _portrait,
                Options = new[]
                {
                    new DialogChoiceOptionData
                    {
                        label = $"Offer {DwarfClanDonationLogic.SmallDonationGold} gold",
                        payload = DonateSmallPayload,
                        enabled = true,
                    },
                    new DialogChoiceOptionData
                    {
                        label = $"Offer {DwarfClanDonationLogic.MediumDonationGold} gold",
                        payload = DonateMediumPayload,
                        enabled = true,
                    },
                    new DialogChoiceOptionData
                    {
                        label = $"Offer {DwarfClanDonationLogic.LargeDonationGold} gold",
                        payload = DonateLargePayload,
                        enabled = true,
                    },
                    new DialogChoiceOptionData
                    {
                        label = "Cancel",
                        payload = DeclinePayload,
                        enabled = true,
                    },
                },
            };

            NpcDialogBoxUI.EnsureInstance().ShowChoice(step, OnDonationChoice, Complete);
        }

        void OnDonationChoice(DialogChoiceOptionData option)
        {
            NpcDialogBoxUI.EnsureInstance().Close();

            if (option == null || option.payload == DeclinePayload)
            {
                Complete();
                return;
            }

            int gold = option.payload switch
            {
                DonateSmallPayload => DwarfClanDonationLogic.SmallDonationGold,
                DonateMediumPayload => DwarfClanDonationLogic.MediumDonationGold,
                DonateLargePayload => DwarfClanDonationLogic.LargeDonationGold,
                _ => 0,
            };

            if (!DwarfClanPrestigeService.TryDonateGold(
                    _clan,
                    gold,
                    out int gained,
                    out int total,
                    out string error))
            {
                ShowLine(error ?? "The treasury rejects your offering.");
                return;
            }

            ShowLine(DwarfClanDonationLogic.BuildDonationSuccessLine(gained, total));
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
