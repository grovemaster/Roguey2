using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Dialog;
using JRogue.UI.Gameplay;
using JRogue.UI.Hotbar;
using UnityEngine;

namespace JRogue.Racial
{
    public static class ElementalSpiritMeditationService
    {
        const string CancelPayload = "__cancel__";
        const string ElfCancelPayload = "__elf_cancel__";

        public static bool TryBeginMeditation(ElementalSpiritMeditationGateDefinition gate)
        {
            if (!ElementalSpiritMeditationLogic.CanBeginMeditation(out string rejectLine))
            {
                ShowFeedback(gate?.displayName ?? "Meditation Shrine", rejectLine);
                return false;
            }

            List<BaseActor> elves = FairyStonePartyRules.GetEligibleElves();
            if (elves.Count == 0)
            {
                ShowFeedback(gate?.displayName ?? "Meditation Shrine", "No Elf can train elemental spirits.");
                return false;
            }

            if (elves.Count == 1)
            {
                BeginInstancePicker(elves[0], gate);
                return true;
            }

            ShowElfPicker(elves, gate);
            return true;
        }

        static void ShowElfPicker(List<BaseActor> elves, ElementalSpiritMeditationGateDefinition gate)
        {
            var options = new List<DialogChoiceOptionData>(elves.Count + 1);
            for (int i = 0; i < elves.Count; i++)
            {
                BaseActor elf = elves[i];
                options.Add(new DialogChoiceOptionData
                {
                    label = elf.DisplayName,
                    payload = i.ToString(),
                    enabled = ElementalSpiritMeditationLogic.IsElfEligible(
                        elf,
                        out _,
                        out _),
                });
            }

            options.Add(new DialogChoiceOptionData
            {
                label = "Cancel",
                payload = ElfCancelPayload,
                enabled = true,
            });

            var step = new DialogChoiceStep
            {
                SpeakerName = gate.displayName,
                PromptText = "Which Elf will meditate?",
                Portrait = null,
                Options = options,
            };

            NpcDialogBoxUI.EnsureInstance().ShowChoice(step, option => OnElfSelected(elves, gate, option));
        }

        static void OnElfSelected(
            List<BaseActor> elves,
            ElementalSpiritMeditationGateDefinition gate,
            DialogChoiceOptionData option)
        {
            NpcDialogBoxUI.EnsureInstance().Close();

            if (option == null
                || option.payload == ElfCancelPayload
                || string.IsNullOrWhiteSpace(option.payload)
                || !int.TryParse(option.payload, out int elfIndex)
                || elfIndex < 0
                || elfIndex >= elves.Count)
            {
                return;
            }

            BeginInstancePicker(elves[elfIndex], gate);
        }

        static void BeginInstancePicker(BaseActor elf, ElementalSpiritMeditationGateDefinition gate)
        {
            if (!ElementalSpiritMeditationLogic.IsElfEligible(elf, out ElementalSpiritContractsRuntime runtime, out string rejectLine))
            {
                ShowFeedback(gate.displayName, rejectLine);
                return;
            }

            IReadOnlyList<ElementalSpiritContractPreset> roster = runtime.ContractedSpirits;
            var options = new List<DialogChoiceOptionData>(roster.Count + 1);

            for (int i = 0; i < roster.Count; i++)
            {
                ElementalSpiritContractPreset preset = roster[i];
                if (preset?.spirit == null)
                    continue;

                preset.EnsureInstanceId();
                bool capped = ElementalSpiritProgressionLogic.IsCappedForXpGain(elf, preset);
                bool affordable = !capped && ElementalSpiritMeditationLogic.CanAffordGate(elf, gate, out _);
                string label = ElementalSpiritProgressionLogic.FormatProgressLine(elf, preset, roster);
                if (!SpiritImprintUpgradeLogic.IsCostEmpty(gate.cost))
                    label += $", {SpiritImprintUpgradeLogic.FormatCostShort(gate.cost)}";

                options.Add(new DialogChoiceOptionData
                {
                    label = label,
                    payload = preset.contractInstanceId,
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
                SpeakerName = gate.displayName,
                PromptText = BuildInstancePrompt(elf, gate),
                Portrait = null,
                Options = options,
            };

            NpcDialogBoxUI.EnsureInstance().ShowChoice(step, option => OnInstanceSelected(elf, gate, option));
        }

        static string BuildInstancePrompt(BaseActor elf, ElementalSpiritMeditationGateDefinition gate)
        {
            string xpLine = gate.spiritXpAward > 0
                ? $"Deepen your bond (+{gate.spiritXpAward} spirit experience)."
                : "Deepen your bond with a contracted spirit.";

            if (!SpiritImprintUpgradeLogic.IsCostEmpty(gate.cost))
                xpLine += $"\nCost: {SpiritImprintUpgradeLogic.FormatCostShort(gate.cost)}.";

            return $"{elf.DisplayName} — choose a spirit to train.\n{xpLine}";
        }

        static void OnInstanceSelected(
            BaseActor elf,
            ElementalSpiritMeditationGateDefinition gate,
            DialogChoiceOptionData option)
        {
            NpcDialogBoxUI.EnsureInstance().Close();

            if (option == null
                || option.payload == CancelPayload
                || string.IsNullOrWhiteSpace(option.payload))
            {
                return;
            }

            string instanceId = option.payload.Trim();
            if (!ElementalSpiritMeditationLogic.IsElfEligible(elf, out ElementalSpiritContractsRuntime runtime, out string rejectLine))
            {
                ShowFeedback(gate.displayName, rejectLine);
                return;
            }

            if (!runtime.TryGetPreset(instanceId, out ElementalSpiritContractPreset preset))
            {
                ShowFeedback(gate.displayName, "Spirit contract not found.");
                return;
            }

            if (ElementalSpiritProgressionLogic.IsCappedForXpGain(elf, preset))
            {
                int cap = ElementalSpiritProgressionLogic.GetEffectiveLevelCap(elf, preset);
                ShowFeedback(
                    gate.displayName,
                    $"Your bond with {ElementalSpiritDisplayNames.GetDisplayLabel(preset, runtime.ContractedSpirits)} cannot deepen until you grow stronger (level {cap}).");
                return;
            }

            if (!ElementalSpiritMeditationLogic.TryPayGateCost(elf, gate, out string payFailure))
            {
                ShowFeedback(gate.displayName, payFailure ?? "You cannot afford this meditation.");
                return;
            }

            string spiritName = ElementalSpiritDisplayNames.GetDisplayLabel(preset, runtime.ContractedSpirits);
            if (!ElementalSpiritMeditationLogic.TryAwardSpiritExperience(
                    elf,
                    runtime,
                    instanceId,
                    gate.spiritXpAward,
                    gate.gateId,
                    out ElementalSpiritMeditationAwardResult award,
                    out string awardFailure))
            {
                ShowFeedback(gate.displayName, awardFailure ?? "Meditation failed.");
                return;
            }

            AbilityHotbarUI.Instance?.RefreshAll();

            string feedback = award.LevelsGained > 0
                ? $"{spiritName} reached contract level {award.FinalContractLevel}!"
                : $"{spiritName} gained {award.XpAwarded} bond experience.";

            ShowFeedback(gate.displayName, feedback);
        }

        static void ShowFeedback(string speaker, string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return;

            Debug.Log($"{ElementalSpiritMeditationLogic.LogPrefix} {line}");
            var step = new DialogLineStep
            {
                SpeakerName = speaker,
                ResolvedText = line,
                Portrait = null,
            };
            NpcDialogBoxUI.EnsureInstance().ShowLine(step, () => NpcDialogBoxUI.EnsureInstance().Close());
        }
    }
}
