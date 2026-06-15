using JRogue.Actors;
using JRogue.Controller.Npc;
using JRogue.Dialog;
using JRogue.Manager.Party;
using JRogue.Quest;
using JRogue.Racial;
using JRogue.Stats;
using JRogue.Stats.Racial;
using JRogue.UI.Gameplay;
using JRogue.World.Generation;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace JRogue.Dialog
{
    public sealed class HumanPriestShrineDialogSession
    {
        const string AcceptPayload = "__accept__";
        const string TurnInPayload = "__turn_in__";
        const string CancelPayload = "__cancel__";
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
                + $"Piety: {piety}. Open K to prepare devotions before you descend.");
        }

        void ShowPriestMenu()
        {
            HumanPriestCovenantRuntime covenant = ResolveCovenantRuntime();
            int piety = covenant != null ? covenant.Piety : 0;
            int slots = HumanPriestPietyService.ResolveDevotionSlotCap(covenant);
            bool canTakeVows = covenant != null
                && covenant.IsCommittedPriest
                && covenant.PenanceDebt <= 0;

            var options = new List<DialogChoiceOptionData>
            {
                new()
                {
                    label = "Take vows before descent",
                    payload = VowsPayload,
                    enabled = canTakeVows,
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
                PromptText =
                    $"Argent Vigil shrine. Piety {piety}. Devotion slots: {slots}. "
                    + "Prepare devotions with K in town.",
                Portrait = _portrait,
                Options = options.ToArray(),
            };

            NpcDialogBoxUI.EnsureInstance().ShowChoice(step, OnPriestMenuChoice, Complete);
        }

        void OnPriestMenuChoice(DialogChoiceOptionData option)
        {
            if (option == null || option.payload == CancelPayload)
            {
                NpcDialogBoxUI.EnsureInstance().Close();
                Complete();
                return;
            }

            if (option.payload == VowsPayload)
            {
                if (!SafeZonePolicyService.TryAllowHumanPriestShrineQuestChange(out string denyReason))
                {
                    NpcDialogBoxUI.EnsureInstance().Close();
                    ShowLine(denyReason ?? "Cannot take vows here.");
                    return;
                }

                ShowVowPicker();
                return;
            }

            NpcDialogBoxUI.EnsureInstance().Close();

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

        void ShowVowPicker(string statusMessage = null)
        {
            var options = new List<DialogChoiceOptionData>();
            HumanPriestCovenantRuntime covenant = ResolveCovenantRuntime();
            var activeVowIds = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
            int activeCount = 0;

            if (covenant?.ActiveVows != null)
            {
                for (int i = 0; i < covenant.ActiveVows.Count; i++)
                {
                    PriestActiveVowState state = covenant.ActiveVows[i];
                    if (state == null || state.failed || state.completed || string.IsNullOrWhiteSpace(state.vowId))
                        continue;

                    if (activeVowIds.Add(state.vowId.Trim()))
                        activeCount++;
                }
            }

            bool atVowCap = activeCount >= 3;

            if (PatronGodCatalogService.TryGetGod(covenant?.PatronGodId, out PatronGodDefinition god)
                && god.vowIds != null)
            {
                for (int i = 0; i < god.vowIds.Count; i++)
                {
                    string vowId = god.vowIds[i];
                    if (!PriestVowCatalogService.TryGetVow(vowId, out PriestVowDefinition vow))
                        continue;

                    bool alreadyActive = activeVowIds.Contains(vowId);
                    options.Add(new DialogChoiceOptionData
                    {
                        label = alreadyActive
                            ? $"{vow.displayName} ({vow.scope}) — chosen"
                            : $"{vow.displayName} ({vow.scope})",
                        payload = vowId,
                        enabled = !alreadyActive && !atVowCap,
                    });
                }
            }

            options.Add(new DialogChoiceOptionData
            {
                label = activeCount > 0 ? $"Done ({activeCount} vow{(activeCount == 1 ? string.Empty : "s")} chosen)" : "Done",
                payload = CancelPayload,
                enabled = true,
            });

            var step = new DialogChoiceStep
            {
                SpeakerName = _displayName,
                PromptText = BuildVowPickerPrompt(activeCount, statusMessage),
                Portrait = _portrait,
                Options = options.ToArray(),
            };

            NpcDialogBoxUI.EnsureInstance().ShowChoice(step, OnVowChoice, Complete);
        }

        static string BuildVowPickerPrompt(int activeCount, string statusMessage)
        {
            var sb = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(statusMessage))
            {
                sb.Append(statusMessage.Trim());
                sb.Append("\n\n");
            }

            sb.Append("Select up to three vows for this delve (party vows bind allies; only you are judged).");
            if (activeCount > 0)
                sb.Append($"\n\nChosen: {activeCount}/3.");
            else
                sb.Append("\n\nNo vows chosen yet.");

            return sb.ToString();
        }

        void OnVowChoice(DialogChoiceOptionData option)
        {
            if (option == null)
            {
                NpcDialogBoxUI.EnsureInstance().Close();
                Complete();
                return;
            }

            if (option.payload == CancelPayload)
            {
                NpcDialogBoxUI.EnsureInstance().Close();
                ShowLine("May the Vigil watch your path.");
                return;
            }

            if (!HumanPriestVowService.TrySelectVows(
                    _speaker,
                    BuildVowSelectionWithAdded(_speaker, option.payload),
                    out string error))
            {
                NpcDialogBoxUI.EnsureInstance().Close();
                ShowLine(error ?? "Cannot take that vow.");
                return;
            }

            string vowLabel = option.payload;
            if (PriestVowCatalogService.TryGetVow(option.payload, out PriestVowDefinition vow))
                vowLabel = vow.displayName;

            ShowVowPicker($"Vow recorded: {vowLabel}.");
        }

        HumanPriestCovenantRuntime ResolveCovenantRuntime()
        {
            HumanPriestCovenantRuntime covenant = _speaker.GetComponent<HumanPriestCovenantRuntime>();
            CharacterStats stats = _speaker.GetComponent<CharacterStats>();
            if (covenant != null && covenant.IsCommittedPriest)
                return covenant;

            if (stats == null || stats.humanClass != HumanClass.Priest)
                return covenant;

            HumanPriestCovenantService.InitializeOnCommit(
                _speaker.gameObject,
                HumanPriestShrineIds.ArgentVigilGodId,
                out _);
            return _speaker.GetComponent<HumanPriestCovenantRuntime>();
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
