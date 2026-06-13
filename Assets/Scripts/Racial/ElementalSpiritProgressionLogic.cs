using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Stats;
using UnityEngine;

namespace JRogue.Racial
{
    public readonly struct ElementalSpiritXpProgress
    {
        public int ContractLevel { get; }
        public int ContractExperience { get; }
        public int XpToNextLevel { get; }
        public int EffectiveCap { get; }
        public bool IsCappedForXpGain { get; }

        public ElementalSpiritXpProgress(
            int contractLevel,
            int contractExperience,
            int xpToNextLevel,
            int effectiveCap,
            bool isCappedForXpGain)
        {
            ContractLevel = contractLevel;
            ContractExperience = contractExperience;
            XpToNextLevel = xpToNextLevel;
            EffectiveCap = effectiveCap;
            IsCappedForXpGain = isCappedForXpGain;
        }
    }

    public static class ElementalSpiritProgressionLogic
    {
        static IElementalSpiritLevelCapPolicy _capPolicy = CharacterLevelSpiritCapPolicy.Instance;

        public static void SetCapPolicyForTests(IElementalSpiritLevelCapPolicy policy) =>
            _capPolicy = policy ?? CharacterLevelSpiritCapPolicy.Instance;

        public static void ResetCapPolicyForTests() =>
            _capPolicy = CharacterLevelSpiritCapPolicy.Instance;

        public static ElementalSpiritLevelCurve ResolveCurve(ElementalSpiritDefinition spirit)
        {
            if (spirit?.levelCurve != null)
                return spirit.levelCurve;

            return ElementalSpiritProgressionConfig.Load()?.defaultLevelCurve;
        }

        public static int GetEffectiveLevelCap(BaseActor elf, ElementalSpiritContractPreset instance)
        {
            if (elf == null || instance?.spirit == null)
                return 1;

            CharacterStats stats = elf.stats;
            return _capPolicy.ResolveEffectiveCap(stats, instance, instance.spirit);
        }

        public static ElementalSpiritXpProgress GetXpProgress(BaseActor elf, ElementalSpiritContractPreset instance)
        {
            if (instance?.spirit == null)
                return default;

            int contractLevel = Mathf.Max(1, instance.contractLevel);
            int effectiveCap = GetEffectiveLevelCap(elf, instance);
            ElementalSpiritLevelCurve curve = ResolveCurve(instance.spirit);
            int xpToNext = curve != null ? curve.GetXpRequiredForNextLevel(contractLevel) : int.MaxValue;
            bool capped = contractLevel >= effectiveCap;

            return new ElementalSpiritXpProgress(
                contractLevel,
                Mathf.Max(0, instance.contractExperience),
                xpToNext,
                effectiveCap,
                capped);
        }

        public static bool IsCappedForXpGain(BaseActor elf, ElementalSpiritContractPreset instance) =>
            GetXpProgress(elf, instance).IsCappedForXpGain;

        public static string BuildInstanceDisplayName(
            ElementalSpiritContractPreset instance,
            IReadOnlyList<ElementalSpiritContractPreset> roster)
        {
            if (instance?.spirit == null)
                return "Spirit";

            string spiritName = string.IsNullOrWhiteSpace(instance.spirit.displayName)
                ? instance.spirit.spiritId
                : instance.spirit.displayName.Trim();

            if (roster == null)
                return spiritName;

            string spiritId = instance.spirit.spiritId ?? string.Empty;
            int index = 0;
            for (int i = 0; i < roster.Count; i++)
            {
                ElementalSpiritContractPreset row = roster[i];
                if (row?.spirit == null || row.spirit.spiritId != spiritId)
                    continue;

                index++;
                if (ReferenceEquals(row, instance) || row.contractInstanceId == instance.contractInstanceId)
                {
                    return index > 1 ? $"{spiritName} ({index})" : spiritName;
                }
            }

            return spiritName;
        }

        public static string FormatProgressLine(
            BaseActor elf,
            ElementalSpiritContractPreset instance,
            IReadOnlyList<ElementalSpiritContractPreset> roster = null)
        {
            ElementalSpiritXpProgress progress = GetXpProgress(elf, instance);
            string name = BuildInstanceDisplayName(instance, roster);

            if (progress.IsCappedForXpGain)
            {
                return $"{name} — Lv {progress.ContractLevel} (cap {progress.EffectiveCap})";
            }

            if (progress.XpToNextLevel == int.MaxValue)
            {
                return $"{name} — Lv {progress.ContractLevel} (max)";
            }

            return $"{name} — Lv {progress.ContractLevel} · {progress.ContractExperience}/{progress.XpToNextLevel} XP";
        }
    }
}
