using System.Collections.Generic;

namespace JRogue.Racial
{
    public static class ElementalSpiritDisplayNames
    {
        public const int MaxNicknameLength = 24;

        public static string GetSpiritTypeName(ElementalSpiritDefinition spirit)
        {
            if (spirit == null)
                return "Spirit";

            return string.IsNullOrWhiteSpace(spirit.displayName)
                ? spirit.spiritId ?? "Spirit"
                : spirit.displayName.Trim();
        }

        public static string GetSortName(ElementalSpiritDefinition spirit) => GetSpiritTypeName(spirit);

        public static string GetCanonicalInstanceName(
            ElementalSpiritContractPreset instance,
            IReadOnlyList<ElementalSpiritContractPreset> roster)
        {
            if (instance?.spirit == null)
                return "Spirit";

            string spiritName = GetSpiritTypeName(instance.spirit);
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
                    return index > 1 ? $"{spiritName} ({index})" : spiritName;
            }

            return spiritName;
        }

        public static string GetDisplayLabel(
            ElementalSpiritContractPreset instance,
            IReadOnlyList<ElementalSpiritContractPreset> roster)
        {
            string nickname = NormalizeNickname(instance?.nickname);
            if (!string.IsNullOrEmpty(nickname))
                return nickname;

            return GetCanonicalInstanceName(instance, roster);
        }

        public static string BuildSummonHotbarLabel(
            ElementalSpiritContractPreset preset,
            IReadOnlyList<ElementalSpiritContractPreset> roster,
            bool summoned)
        {
            string displayLabel = GetDisplayLabel(preset, roster);
            return summoned ? $"{displayLabel} — Dismiss" : $"{displayLabel} — Summon";
        }

        public static string NormalizeNickname(string nickname)
        {
            if (string.IsNullOrWhiteSpace(nickname))
                return string.Empty;

            string trimmed = nickname.Trim();
            if (trimmed.Length == 0)
                return string.Empty;

            trimmed = trimmed.Replace("\r", string.Empty).Replace("\n", string.Empty);
            if (trimmed.Length > MaxNicknameLength)
                trimmed = trimmed.Substring(0, MaxNicknameLength);

            return trimmed;
        }
    }
}
