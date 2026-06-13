using System;
using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Dialog;
using JRogue.Item;
using JRogue.UI.Gameplay;
using UnityEngine;

namespace JRogue.Racial
{
    public static class SoulBeastRitualService
    {
        const string CancelPayload = "__cancel__";
        const string PerformerCancelPayload = "__performer_cancel__";
        const string DeclineContractPayload = "__decline__";

        static System.Random _rng = new System.Random();

        public static bool TryBeginRitual(SoulBeastRitualGateDefinition gate)
        {
            if (!SoulBeastRitualLogic.CanBeginRitual(out string rejectLine))
            {
                ShowFeedback(gate?.displayName ?? "Soul Beast Ritual Circle", rejectLine);
                return false;
            }

            List<BaseActor> performers = SoulBeastPartyRules.GetEligibleBeastmen(requireUnbonded: true);
            if (performers.Count == 0)
            {
                ShowFeedback(gate?.displayName ?? "Soul Beast Ritual Circle", "No unbonded Beastman can perform a ritual.");
                return false;
            }

            if (performers.Count == 1)
            {
                BeginRitualTypePicker(performers[0], gate, Array.Empty<ItemData>());
                return true;
            }

            ShowPerformerPicker(performers, gate);
            return true;
        }

        public static void TryBeginRitualDev() =>
            TryBeginRitual(LoadDefaultGate());

        static void ShowPerformerPicker(List<BaseActor> performers, SoulBeastRitualGateDefinition gate)
        {
            var options = new List<DialogChoiceOptionData>(performers.Count + 1);
            for (int i = 0; i < performers.Count; i++)
            {
                BaseActor performer = performers[i];
                options.Add(new DialogChoiceOptionData
                {
                    label = performer.DisplayName,
                    payload = i.ToString(),
                    enabled = SoulBeastPartyRules.IsEligibleBeastman(performer, requireUnbonded: true, out _),
                });
            }

            options.Add(new DialogChoiceOptionData
            {
                label = "Cancel",
                payload = PerformerCancelPayload,
                enabled = true,
            });

            var step = new DialogChoiceStep
            {
                SpeakerName = gate.displayName,
                PromptText = "Which Beastman will perform the ritual?",
                Portrait = null,
                Options = options,
            };

            NpcDialogBoxUI.EnsureInstance().ShowChoice(step, option => OnPerformerSelected(performers, gate, option));
        }

        static void OnPerformerSelected(
            List<BaseActor> performers,
            SoulBeastRitualGateDefinition gate,
            DialogChoiceOptionData option)
        {
            NpcDialogBoxUI.EnsureInstance().Close();

            if (option == null
                || option.payload == PerformerCancelPayload
                || string.IsNullOrWhiteSpace(option.payload)
                || !int.TryParse(option.payload, out int performerIndex)
                || performerIndex < 0
                || performerIndex >= performers.Count)
            {
                return;
            }

            BeginRitualTypePicker(performers[performerIndex], gate, Array.Empty<ItemData>());
        }

        static void BeginRitualTypePicker(
            BaseActor performer,
            SoulBeastRitualGateDefinition gate,
            IReadOnlyList<ItemData> offerings)
        {
            if (!SoulBeastPartyRules.IsEligibleBeastman(performer, requireUnbonded: true, out string rejectLine))
            {
                ShowFeedback(gate.displayName, rejectLine);
                return;
            }

            if (gate?.ritualTypes == null || gate.ritualTypes.Count == 0)
            {
                ShowFeedback(gate.displayName, "No ritual types are configured.");
                return;
            }

            var options = new List<DialogChoiceOptionData>(gate.ritualTypes.Count + 1);
            for (int i = 0; i < gate.ritualTypes.Count; i++)
            {
                SoulBeastRitualTypeDefinition ritualType = gate.ritualTypes[i];
                if (ritualType == null)
                    continue;

                options.Add(new DialogChoiceOptionData
                {
                    label = string.IsNullOrWhiteSpace(ritualType.displayName)
                        ? ritualType.ritualTypeId
                        : ritualType.displayName,
                    payload = i.ToString(),
                    enabled = true,
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
                PromptText = $"{performer.DisplayName} — choose a ritual type.",
                Portrait = null,
                Options = options,
            };

            NpcDialogBoxUI.EnsureInstance().ShowChoice(
                step,
                option => OnRitualTypeSelected(performer, gate, offerings, option));
        }

        static void OnRitualTypeSelected(
            BaseActor performer,
            SoulBeastRitualGateDefinition gate,
            IReadOnlyList<ItemData> offerings,
            DialogChoiceOptionData option)
        {
            NpcDialogBoxUI.EnsureInstance().Close();

            if (option == null
                || option.payload == CancelPayload
                || string.IsNullOrWhiteSpace(option.payload)
                || !int.TryParse(option.payload, out int typeIndex)
                || gate?.ritualTypes == null
                || typeIndex < 0
                || typeIndex >= gate.ritualTypes.Count)
            {
                return;
            }

            SoulBeastRitualTypeDefinition ritualType = gate.ritualTypes[typeIndex];
            PerformRitual(performer, gate, ritualType, offerings ?? Array.Empty<ItemData>());
        }

        static void PerformRitual(
            BaseActor performer,
            SoulBeastRitualGateDefinition gate,
            SoulBeastRitualTypeDefinition ritualType,
            IReadOnlyList<ItemData> offerings)
        {
            SoulBeastRegistry registry = SoulBeastRegistryService.Registry;
            List<SoulBeastWeightedCandidate> pool =
                SoulBeastRitualLogic.BuildWeightedPool(registry, ritualType, offerings);

            if (pool.Count == 0)
            {
                ShowFeedback(gate.displayName, "The ritual finds no answering soul.");
                return;
            }

            SoulBeastDefinition appeared = SoulBeastRitualLogic.RollAppearance(
                pool,
                ritualType.noneOutcomeWeight,
                _rng);

            if (appeared == null)
            {
                ShowFeedback(gate.displayName, "The ritual completes in silence. No Soul Beast appears.");
                return;
            }

            ShowContractDialog(performer, gate, appeared);
        }

        static void ShowContractDialog(
            BaseActor performer,
            SoulBeastRitualGateDefinition gate,
            SoulBeastDefinition beast)
        {
            string beastName = string.IsNullOrWhiteSpace(beast.displayName)
                ? beast.soulBeastId
                : beast.displayName.Trim();
            string body = string.IsNullOrWhiteSpace(beast.description)
                ? $"{beastName} ({beast.soulBeastType}) seeks a contract."
                : $"{beastName} ({beast.soulBeastType})\n{beast.description.Trim()}";

            var options = new List<DialogChoiceOptionData>
            {
                new DialogChoiceOptionData
                {
                    label = "Form contract",
                    payload = "accept",
                    enabled = true,
                },
                new DialogChoiceOptionData
                {
                    label = "Send it away",
                    payload = DeclineContractPayload,
                    enabled = true,
                },
            };

            var step = new DialogChoiceStep
            {
                SpeakerName = "A Soul Beast appears",
                PromptText = body,
                Portrait = null,
                Options = options,
            };

            NpcDialogBoxUI.EnsureInstance().ShowChoice(
                step,
                option => OnContractChoice(performer, gate, beast, option));
        }

        static void OnContractChoice(
            BaseActor performer,
            SoulBeastRitualGateDefinition gate,
            SoulBeastDefinition beast,
            DialogChoiceOptionData option)
        {
            NpcDialogBoxUI.EnsureInstance().Close();

            if (option == null || option.payload == DeclineContractPayload)
            {
                ShowFeedback(gate.displayName, "The Soul Beast fades away.");
                return;
            }

            BeastmanSoulBeastRuntime runtime = performer.GetComponent<BeastmanSoulBeastRuntime>();
            if (runtime == null)
            {
                ShowFeedback(gate.displayName, "The contract fails.");
                return;
            }

            if (!runtime.TryFormContract(beast, out string failureReason))
            {
                ShowFeedback(gate.displayName, failureReason ?? "The contract fails.");
                return;
            }

            string beastName = string.IsNullOrWhiteSpace(beast.displayName)
                ? beast.soulBeastId
                : beast.displayName.Trim();
            ShowFeedback(
                gate.displayName,
                $"{performer.DisplayName} forms a permanent contract with {beastName}!");
        }

        static void ShowFeedback(string speaker, string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return;

            Debug.Log($"[SoulBeastRitual] {line}");
            var feedback = new DialogLineStep
            {
                SpeakerName = speaker,
                ResolvedText = line,
                Portrait = null,
            };
            NpcDialogBoxUI.EnsureInstance().ShowLine(feedback, () => NpcDialogBoxUI.EnsureInstance().Close());
        }

        static SoulBeastRitualGateDefinition LoadDefaultGate()
        {
            SoulBeastRitualGateDefinition gate =
                Resources.Load<SoulBeastRitualGateDefinition>("Racial/Beastman/SoulBeastRitualGate_Town");
#if UNITY_EDITOR
            if (gate == null)
                gate = UnityEditor.AssetDatabase.LoadAssetAtPath<SoulBeastRitualGateDefinition>(
                    "Assets/Data/Racial/Beastman/SoulBeastRitualGate_Town.asset");
#endif
            return gate;
        }

#if UNITY_EDITOR
        public static void SetRandomForTests(System.Random rng) => _rng = rng ?? new System.Random();
#endif
    }
}
