using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Dialog;
using JRogue.Manager.Party;
using JRogue.Stats;
using JRogue.Stats.Racial;
using JRogue.World.Generation;
using UnityEngine;

namespace JRogue.Racial
{
    public readonly struct ElementalSpiritMeditationAwardResult
    {
        public int XpAwarded { get; }
        public int LevelsGained { get; }
        public int FinalContractLevel { get; }
        public int FinalContractExperience { get; }

        public ElementalSpiritMeditationAwardResult(
            int xpAwarded,
            int levelsGained,
            int finalContractLevel,
            int finalContractExperience)
        {
            XpAwarded = xpAwarded;
            LevelsGained = levelsGained;
            FinalContractLevel = finalContractLevel;
            FinalContractExperience = finalContractExperience;
        }
    }

    public static class ElementalSpiritMeditationLogic
    {
        public const string LogPrefix = "[SpiritMeditation]";

        public static bool CanBeginMeditation(out string rejectLine)
        {
            rejectLine = null;

            if (!SafeZonePolicyService.IsSafeZoneForActiveParty())
            {
                rejectLine = "You can only meditate with your spirits in town.";
                return false;
            }

            if (!FairyStonePartyRules.PartyHasElf())
            {
                rejectLine = "No Elf in the party can commune with elemental spirits.";
                return false;
            }

            return true;
        }

        public static bool IsElfEligible(BaseActor elf, out ElementalSpiritContractsRuntime runtime, out string rejectLine)
        {
            runtime = null;
            rejectLine = null;

            if (elf == null)
            {
                rejectLine = "No Elf selected.";
                return false;
            }

            CharacterStats stats = elf.stats;
            if (stats == null || stats.race != Race.Elf)
            {
                rejectLine = "Target is not an Elf.";
                return false;
            }

            if (stats.racialSubsystem != RacialSubsystemKind.ElfElementalContracts)
            {
                rejectLine = "This Elf cannot train elemental spirits.";
                return false;
            }

            runtime = elf.GetComponent<ElementalSpiritContractsRuntime>();
            if (runtime == null)
            {
                rejectLine = "No elemental spirit runtime.";
                return false;
            }

            if (runtime.ContractedSpirits == null || runtime.ContractedSpirits.Count == 0)
            {
                rejectLine = "You have no spirit contracts to nurture.";
                return false;
            }

            return true;
        }

        public static bool CanAffordGate(
            BaseActor payerElf,
            ElementalSpiritMeditationGateDefinition gate,
            out string denyReason)
        {
            denyReason = null;
            if (gate == null)
            {
                denyReason = "No meditation gate.";
                return false;
            }

            GameStoryFlagService.EnsureInstance();
            PartyManager party = PartyManager.Instance;
            IReadOnlyList<BaseActor> members = party != null ? party.partyMembers : null;
            List<BaseActor> ordered = SpiritImprintUpgradeLogic.OrderPartyMembersForPayment(members, payerElf);
            return SpiritImprintUpgradeLogic.CanAfford(gate.cost, ordered, GameStoryFlagService.Instance, out denyReason);
        }

        public static bool TryPayGateCost(
            BaseActor payerElf,
            ElementalSpiritMeditationGateDefinition gate,
            out string failureReason)
        {
            failureReason = null;
            if (gate == null)
            {
                failureReason = "No meditation gate.";
                return false;
            }

            GameStoryFlagService.EnsureInstance();
            PartyManager party = PartyManager.Instance;
            IReadOnlyList<BaseActor> members = party != null ? party.partyMembers : null;
            List<BaseActor> ordered = SpiritImprintUpgradeLogic.OrderPartyMembersForPayment(members, payerElf);
            return SpiritImprintUpgradeLogic.TryPayCost(gate.cost, ordered, GameStoryFlagService.Instance, out failureReason);
        }

        public static bool TryAwardSpiritExperience(
            BaseActor elf,
            ElementalSpiritContractsRuntime runtime,
            string contractInstanceId,
            int amount,
            string source,
            out ElementalSpiritMeditationAwardResult result,
            out string failureReason)
        {
            result = default;
            failureReason = null;

            if (!IsElfEligible(elf, out ElementalSpiritContractsRuntime eligibleRuntime, out failureReason)
                || eligibleRuntime != runtime)
            {
                failureReason ??= "Invalid Elf runtime.";
                return false;
            }

            if (!runtime.TryGetPreset(contractInstanceId, out ElementalSpiritContractPreset preset)
                || preset.spirit == null)
            {
                failureReason = "Spirit contract not found.";
                return false;
            }

            if (amount <= 0)
            {
                failureReason = "Invalid spirit experience amount.";
                return false;
            }

            if (ElementalSpiritProgressionLogic.IsCappedForXpGain(elf, preset))
            {
                failureReason = $"Spirit level cannot exceed your level ({ElementalSpiritProgressionLogic.GetEffectiveLevelCap(elf, preset)}).";
                return false;
            }

            preset.contractExperience = Mathf.Max(0, preset.contractExperience) + amount;
            int levelsGained = runtime.ResolveContractLevelUps(
                contractInstanceId,
                ElementalSpiritProgressionLogic.GetEffectiveLevelCap(elf, preset),
                ElementalSpiritProgressionLogic.ResolveCurve(preset.spirit));

            result = new ElementalSpiritMeditationAwardResult(
                amount,
                levelsGained,
                preset.contractLevel,
                preset.contractExperience);

            Debug.Log(
                $"{LogPrefix} {elf.DisplayName} +{amount} from {source} → {contractInstanceId} " +
                $"L{preset.contractLevel} ({preset.contractExperience}) cap={ElementalSpiritProgressionLogic.GetEffectiveLevelCap(elf, preset)}");

            return true;
        }
    }
}
