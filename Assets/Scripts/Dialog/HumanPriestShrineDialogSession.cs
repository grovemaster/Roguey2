using JRogue.Actors;
using JRogue.Controller.Npc;
using JRogue.Dialog;
using JRogue.Manager.Party;
using JRogue.Quest;
using JRogue.Racial;
using JRogue.Stats;
using JRogue.Stats.Racial;
using JRogue.UI.Gameplay;
using System.Collections.Generic;
using UnityEngine;

namespace JRogue.Dialog
{
    public sealed class HumanPriestShrineDialogSession
    {
        const string AcceptPayload = "__accept__";
        const string TurnInPayload = "__turn_in__";
        const string CancelPayload = "__cancel__";
        const string PreparePayload = "__prepare__";
        const string VowsPayload = "__vows__";
        const string ReportVowsPayload = "__report_vows__";
        const string RepentPayload = "__repent__";

        readonly BaseActor _speaker;
        readonly INpcTalkTarget _target;
        readonly PortraitDefinition _portrait;
        readonly string _displayName;
        readonly string _npcId;

        public HumanPriestShrineDialogSession(BaseActor speaker, INpcTalkTarget target)
        {
            QuestService.EnsureRunService();

            _speaker = speaker;
            _target = target;
            _portrait = target.Portrait;
            _displayName = target.Actor != null ? target.Actor.DisplayName : "Shrine Steward";
            _npcId = target.Actor != null ? target.Actor.name : string.Empty;

            NpcController npc = target.Actor as NpcController;
            if (npc != null && !string.IsNullOrWhiteSpace(npc.NpcId))
                _npcId = npc.NpcId;
        }

        public void Start()
        {
            if (!HumanPriestShrineQuestLogic.IsSpeakerHuman(_speaker, out CharacterStats stats, out string rejectLine))
            {
                ShowLine(rejectLine ?? HumanPriestClassCommitService.RaceDenyMessage);
                return;
            }

            if (HumanPriestShrineQuestLogic.IsAlreadyPriest(stats))
            {
                ShowPriestMenu();
                return;
            }

            if (HumanPriestShrineQuestLogic.HasCommittedElsewhere(stats))
            {
                ShowLine(HumanPriestClassCommitService.ClassDenyMessage);
                return;
            }

            if (!HumanPriestClassCommitService.CanBeginPriestInitiation(_speaker, out string essenceLine)
                && !string.IsNullOrEmpty(essenceLine)
                && essenceLine != HumanPriestClassCommitService.RaceDenyMessage
                && essenceLine != HumanPriestClassCommitService.ClassDenyMessage)
            {
                ShowLine(
                    "Relinquish your consumed essences before you swear covenant. "
                    + "A priest cannot consume essences.");
                return;
            }

            QuestService quests = QuestService.Instance;
            if (HumanPriestShrineQuestLogic.TryGetActiveInitiation(_speaker, quests, out _, out _))
            {
                ShowTurnIn();
                return;
            }

            if (HumanPriestShrineQuestLogic.IsInitiationCompleted(_speaker, quests))
            {
                ShowLine("You already serve the Argent Vigil.");
                return;
            }

            ShowOffer();
        }

        void ShowOffer()
        {
            var step = new DialogChoiceStep
            {
                SpeakerName = _displayName,
                PromptText =
                    "The Argent Vigil welcomes those who forsake essence consumption. "
                    + $"Pay {HumanPriestClassCommitService.InitiationGoldCost} gold when you are ready to swear covenant.",
                Portrait = _portrait,
                Options = new[]
                {
                    new DialogChoiceOptionData
                    {
                        label = "Begin initiation",
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

            if (!HumanPriestShrineQuestService.TryAcceptInitiation(_speaker, out _, out string failureReason))
            {
                ShowLine(failureReason ?? "You cannot begin initiation right now.");
                return;
            }

            ShowLine("Your initiation begins. Return when you are ready to pay the offering.");
        }

        void ShowTurnIn()
        {
            var step = new DialogChoiceStep
            {
                SpeakerName = _displayName,
                PromptText =
                    $"Swear covenant to the Argent Vigil for {HumanPriestClassCommitService.InitiationGoldCost} gold?",
                Portrait = _portrait,
                Options = new[]
                {
                    new DialogChoiceOptionData
                    {
                        label = "Pay & swear covenant",
                        payload = TurnInPayload,
                        enabled = HumanPriestClassCommitService.HasInitiationGold(),
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

            if (!HumanPriestClassCommitService.HasInitiationGold())
            {
                ShowLine(HumanPriestClassCommitService.GoldDenyMessage);
                return;
            }

            if (!HumanPriestShrineQuestService.TryCompleteInitiation(
                    _speaker,
                    _npcId,
                    out _,
                    out string failureReason))
            {
                ShowLine(failureReason ?? "Initiation failed.");
                return;
            }

            HumanPriestCovenantRuntime covenant = _speaker.GetComponent<HumanPriestCovenantRuntime>();
            int piety = covenant != null ? covenant.Piety : 0;
            ShowLine(
                $"You swear covenant to the Argent Vigil. Divine Power flows where Soul Power once lived. "
                + $"Piety: {piety}. Prepare devotions before you descend.");
        }

        void ShowPriestMenu()
        {
            HumanPriestCovenantRuntime covenant = _speaker.GetComponent<HumanPriestCovenantRuntime>();
            int piety = covenant != null ? covenant.Piety : 0;
            int slots = HumanPriestPietyService.ResolveDevotionSlotCap(covenant);

            var options = new List<DialogChoiceOptionData>
            {
                new()
                {
                    label = "Prepare devotions",
                    payload = PreparePayload,
                    enabled = true,
                },
                new()
                {
                    label = "Take vows before descent",
                    payload = VowsPayload,
                    enabled = covenant != null && covenant.PenanceDebt <= 0,
                },
                new()
                {
                    label = "Report fulfilled vows",
                    payload = ReportVowsPayload,
                    enabled = covenant != null && covenant.ActiveVows.Count > 0,
                },
                new()
                {
                    label = "Repent (clear penance)",
                    payload = RepentPayload,
                    enabled = covenant != null && covenant.PenanceDebt > 0,
                },
                new()
                {
                    label = "Leave",
                    payload = CancelPayload,
                    enabled = true,
                },
            };

            var step = new DialogChoiceStep
            {
                SpeakerName = _displayName,
                PromptText = $"Argent Vigil shrine. Piety {piety}. Devotion slots: {slots}.",
                Portrait = _portrait,
                Options = options.ToArray(),
            };

            NpcDialogBoxUI.EnsureInstance().ShowChoice(step, OnPriestMenuChoice, Complete);
        }

        void OnPriestMenuChoice(DialogChoiceOptionData option)
        {
            NpcDialogBoxUI.EnsureInstance().Close();

            if (option == null || option.payload == CancelPayload)
            {
                Complete();
                return;
            }

            if (option.payload == PreparePayload)
            {
                ShowPrepareDevotions();
                return;
            }

            if (option.payload == VowsPayload)
            {
                ShowVowPicker();
                return;
            }

            if (option.payload == ReportVowsPayload)
            {
                if (!HumanPriestVowService.TryReportVowsAtShrine(_speaker, out string summary, out string error))
                {
                    ShowLine(error ?? "Cannot report vows.");
                    return;
                }

                ShowLine(summary);
                return;
            }

            if (option.payload == RepentPayload)
            {
                HumanPriestCovenantRuntime covenant = _speaker.GetComponent<HumanPriestCovenantRuntime>();
                covenant?.ClearPenance();
                ShowLine("The shrine accepts your repentance. Penance is cleared.");
            }
        }

        void ShowPrepareDevotions()
        {
            if (!HumanPriestDevotionLoadoutService.TryAllowEdit(_speaker, out string deny))
            {
                ShowLine(deny ?? "You cannot prepare devotions here.");
                return;
            }

            HumanPriestCovenantRuntime covenant = _speaker.GetComponent<HumanPriestCovenantRuntime>();
            int cap = HumanPriestPietyService.ResolveDevotionSlotCap(covenant);
            HumanPriestDevotionRuntime devotion = _speaker.GetComponent<HumanPriestDevotionRuntime>();

            var options = new List<DialogChoiceOptionData>();
            IReadOnlyList<PriestInvocationDefinition> all = PriestInvocationCatalogService.GetAllInvocations();
            CharacterStats stats = _speaker.GetComponent<CharacterStats>();

            for (int i = 0; i < all.Count; i++)
            {
                PriestInvocationDefinition invocation = all[i];
                if (invocation == null)
                    continue;

                bool unlocked = HumanPriestPietyService.IsInvocationUnlocked(stats, covenant, invocation);
                string label = unlocked
                    ? invocation.displayName
                    : $"{invocation.displayName} — {HumanPriestPietyService.BuildLockedReason(stats, covenant, invocation)}";

                options.Add(new DialogChoiceOptionData
                {
                    label = label,
                    payload = invocation.invocationId,
                    enabled = unlocked,
                });
            }

            options.Add(new DialogChoiceOptionData
            {
                label = "Done",
                payload = CancelPayload,
                enabled = true,
            });

            var step = new DialogChoiceStep
            {
                SpeakerName = _displayName,
                PromptText = $"Choose up to {cap} prepared devotions (toggle to equip).",
                Portrait = _portrait,
                Options = options.ToArray(),
            };

            NpcDialogBoxUI.EnsureInstance().ShowChoice(step, OnPrepareChoice, Complete);
        }

        void OnPrepareChoice(DialogChoiceOptionData option)
        {
            NpcDialogBoxUI.EnsureInstance().Close();

            if (option == null || option.payload == CancelPayload)
            {
                HumanPriestHotbarSync.TryAssignEquippedToEmptyMainSlots(_speaker);
                ShowLine("Devotions prepared. Assign them on your ability hotbar if needed.");
                return;
            }

            HumanPriestDevotionRuntime devotion = _speaker.GetComponent<HumanPriestDevotionRuntime>();
            if (devotion == null)
            {
                ShowLine("No devotion runtime.");
                return;
            }

            string id = option.payload;
            if (devotion.EquippedInvocations.Count > 0)
            {
                for (int i = 0; i < devotion.EquippedInvocations.Count; i++)
                {
                    if (devotion.EquippedInvocations[i]?.invocationId == id)
                    {
                        devotion.TryUnequip(id);
                        ShowPrepareDevotions();
                        return;
                    }
                }
            }

            if (!HumanPriestDevotionLoadoutService.TryEquip(_speaker, id, out string error))
            {
                ShowLine(error ?? "Cannot prepare that devotion.");
                return;
            }

            ShowPrepareDevotions();
        }

        void ShowVowPicker()
        {
            var options = new List<DialogChoiceOptionData>
            {
                new()
                {
                    label = "Peacebound (personal)",
                    payload = "vow_peacebound",
                    enabled = true,
                },
                new()
                {
                    label = "Essence abstinence (party)",
                    payload = "vow_essence_abstinence",
                    enabled = true,
                },
                new()
                {
                    label = "Confirm vows & descend",
                    payload = CancelPayload,
                    enabled = true,
                },
            };

            var step = new DialogChoiceStep
            {
                SpeakerName = _displayName,
                PromptText = "Select vows for this delve (party vows bind allies; only you are judged).",
                Portrait = _portrait,
                Options = options.ToArray(),
            };

            NpcDialogBoxUI.EnsureInstance().ShowChoice(step, OnVowChoice, Complete);
        }

        void OnVowChoice(DialogChoiceOptionData option)
        {
            NpcDialogBoxUI.EnsureInstance().Close();

            if (option == null)
            {
                Complete();
                return;
            }

            if (option.payload == CancelPayload)
            {
                ShowLine("May the Vigil watch your path.");
                return;
            }

            if (!HumanPriestVowService.TrySelectVows(
                    _speaker,
                    BuildVowSelectionWithAdded(_speaker, option.payload),
                    out string error))
            {
                ShowLine(error ?? "Cannot take that vow.");
                return;
            }

            ShowLine($"Vow recorded: {option.payload}.");
            ShowVowPicker();
        }

        static List<string> BuildVowSelectionWithAdded(BaseActor speaker, string vowId)
        {
            var ids = new List<string>();
            HumanPriestCovenantRuntime covenant = speaker?.GetComponent<HumanPriestCovenantRuntime>();
            if (covenant?.ActiveVows != null)
            {
                for (int i = 0; i < covenant.ActiveVows.Count; i++)
                {
                    PriestActiveVowState state = covenant.ActiveVows[i];
                    if (state == null || state.failed || state.completed)
                        continue;

                    if (!ids.Contains(state.vowId))
                        ids.Add(state.vowId);
                }
            }

            if (!string.IsNullOrWhiteSpace(vowId) && !ids.Contains(vowId))
                ids.Add(vowId);

            return ids;
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
