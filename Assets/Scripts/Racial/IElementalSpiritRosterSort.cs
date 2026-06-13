using System.Collections.Generic;

namespace JRogue.Racial
{
    public interface IElementalSpiritRosterSort
    {
        void Sort(List<ElementalSpiritContractPreset> rows, IReadOnlyList<ElementalSpiritContractPreset> roster);
    }

    public sealed class LevelDescNameAscRosterSort : IElementalSpiritRosterSort
    {
        public static LevelDescNameAscRosterSort Instance { get; } = new LevelDescNameAscRosterSort();

        public void Sort(List<ElementalSpiritContractPreset> rows, IReadOnlyList<ElementalSpiritContractPreset> roster)
        {
            if (rows == null || rows.Count <= 1)
                return;

            rows.Sort((left, right) => Compare(left, right, roster));
        }

        static int Compare(
            ElementalSpiritContractPreset left,
            ElementalSpiritContractPreset right,
            IReadOnlyList<ElementalSpiritContractPreset> roster)
        {
            int levelLeft = left?.contractLevel ?? 0;
            int levelRight = right?.contractLevel ?? 0;
            int levelCompare = levelRight.CompareTo(levelLeft);
            if (levelCompare != 0)
                return levelCompare;

            string nameLeft = ElementalSpiritDisplayNames.GetSortName(left?.spirit);
            string nameRight = ElementalSpiritDisplayNames.GetSortName(right?.spirit);
            int nameCompare = string.Compare(nameLeft, nameRight, System.StringComparison.OrdinalIgnoreCase);
            if (nameCompare != 0)
                return nameCompare;

            string idLeft = left?.contractInstanceId ?? string.Empty;
            string idRight = right?.contractInstanceId ?? string.Empty;
            return string.Compare(idLeft, idRight, System.StringComparison.Ordinal);
        }
    }

    public static class ElementalSpiritRosterSort
    {
        public static IElementalSpiritRosterSort Default => LevelDescNameAscRosterSort.Instance;

        public static List<ElementalSpiritContractPreset> Apply(
            IReadOnlyList<ElementalSpiritContractPreset> source,
            IElementalSpiritRosterSort sort = null)
        {
            var rows = new List<ElementalSpiritContractPreset>();
            if (source == null)
                return rows;

            for (int i = 0; i < source.Count; i++)
            {
                ElementalSpiritContractPreset preset = source[i];
                if (preset?.spirit != null)
                    rows.Add(preset);
            }

            (sort ?? Default).Sort(rows, source);
            return rows;
        }
    }
}
