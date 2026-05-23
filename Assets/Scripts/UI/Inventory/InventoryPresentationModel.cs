using System;
using System.Collections.Generic;
using System.Linq;
using JRogue.Item;

namespace JRogue.UI.Inventory
{
    /// <summary>
    /// Filters an <see cref="InventoryViewModel"/> and builds sectioned presentation lines plus a flat lettered row list for selection.
    /// </summary>
    public sealed class InventoryPresentationModel
    {
        public readonly struct PresentationLine
        {
            PresentationLine(bool isHeader, string headerRichText, InventoryViewModel.Row row)
            {
                IsSectionHeader = isHeader;
                HeaderRichText = headerRichText ?? string.Empty;
                Row = row;
            }

            public bool IsSectionHeader { get; }

            /// <summary>Valid when <see cref="IsSectionHeader"/>.</summary>
            public string HeaderRichText { get; }

            /// <summary>Meaningful for item lines.</summary>
            public InventoryViewModel.Row Row { get; }

            public static PresentationLine Header(string rich) => new PresentationLine(true, rich, default);

            public static PresentationLine Item(InventoryViewModel.Row r) =>
                new PresentationLine(false, string.Empty, r);
        }

        readonly List<PresentationLine> _lines = new List<PresentationLine>();
        readonly List<InventoryViewModel.Row> _itemRows = new List<InventoryViewModel.Row>();

        InventoryPresentationModel(List<PresentationLine> lines, List<InventoryViewModel.Row> rows)
        {
            _lines = lines;
            _itemRows = rows;
        }

        public IReadOnlyList<PresentationLine> Lines => _lines;
        public IReadOnlyList<InventoryViewModel.Row> ItemRows => _itemRows;

        public static InventoryPresentationModel BuildFiltered(
            InventoryViewModel source,
            ItemCategory? categoryFilter,
            string searchNeedle,
            bool usableOnly,
            bool inCombat,
            InventorySortMode sortMode)
        {
            var lines = new List<PresentationLine>();
            var orderedItems = new List<InventoryViewModel.Row>();

            if (source == null)
                return new InventoryPresentationModel(lines, orderedItems);

            List<InventoryViewModel.Row> allRows = source.Rows.Where(r => r.Item != null).ToList();
            IEnumerable<InventoryViewModel.Row> q = allRows;

            if (categoryFilter.HasValue)
                q = q.Where(r => r.Item.category == categoryFilter.Value);

            if (!string.IsNullOrWhiteSpace(searchNeedle))
            {
                string n = searchNeedle.Trim();
                q = q.Where(r => RowMatchesSearch(r, n));
            }

            if (usableOnly)
                q = q.Where(r => r.IsEquipped || InventoryUsability.AppearsUsableNow(r, inCombat));

            List<InventoryViewModel.Row> pool = q.ToList();

            switch (sortMode)
            {
                case InventorySortMode.FlatByName:
                    pool.Sort((a, b) => string.Compare(
                        a.Item.itemName,
                        b.Item.itemName,
                        StringComparison.OrdinalIgnoreCase));
                    AppendFlat(lines, orderedItems, pool, "<color=#cfd6dd><b>All items</b></color>");
                    break;

                case InventorySortMode.FlatByWeightDesc:
                    pool = pool
                        .OrderByDescending(r => r.StackedWeight)
                        .ThenBy(r => r.Item.itemName ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                        .ToList();
                    AppendFlat(lines, orderedItems, pool, "<color=#cfd6dd><b>All items · by weight</b></color>");
                    break;

                case InventorySortMode.CategoryFavoritesFirst:
                    AppendCategoryGrouped(lines, orderedItems, pool, favoritesFirst: true);
                    break;

                default:
                    AppendCategoryGrouped(lines, orderedItems, pool, favoritesFirst: false);
                    break;
            }

            return new InventoryPresentationModel(lines, orderedItems);
        }

        static void AppendFlat(
            List<PresentationLine> lines,
            List<InventoryViewModel.Row> orderedItems,
            List<InventoryViewModel.Row> rows,
            string headerRich)
        {
            if (rows.Count == 0)
                return;

            lines.Add(PresentationLine.Header(headerRich));

            foreach (InventoryViewModel.Row raw in rows)
            {
                char letter = InventoryViewModel.LetterForIndex(orderedItems.Count);
                InventoryViewModel.Row tagged = raw.WithLetter(letter);
                orderedItems.Add(tagged);
                lines.Add(PresentationLine.Item(tagged));
            }
        }

        static void AppendCategoryGrouped(
            List<PresentationLine> lines,
            List<InventoryViewModel.Row> orderedItems,
            List<InventoryViewModel.Row> pool,
            bool favoritesFirst)
        {
            List<InventoryViewModel.Row> working = pool
                .OrderBy(r => r.Item.itemName ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .ToList();

            IOrderedEnumerable<IGrouping<ItemCategory, InventoryViewModel.Row>> grouped = working
                .GroupBy(r => r.Item.category)
                .OrderBy(g => ItemCategoryRegistry.Get(g.Key).SortOrder)
                .ThenBy(g => g.Key.ToString());

            foreach (IGrouping<ItemCategory, InventoryViewModel.Row> g in grouped)
            {
                IEnumerable<InventoryViewModel.Row> orderedInCat = favoritesFirst
                    ? g.OrderBy(r => IsFavorite(r) ? 0 : 1)
                        .ThenBy(r => r.Item.itemName ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                    : g.OrderBy(r => r.Item.itemName ?? string.Empty, StringComparer.OrdinalIgnoreCase);

                List<InventoryViewModel.Row> inCat = orderedInCat.ToList();
                if (inCat.Count == 0)
                    continue;

                ItemCategoryUiInfo meta = ItemCategoryRegistry.Get(g.Key);
                string shortCut = string.IsNullOrEmpty(meta.FilterShortcutLabel)
                    ? string.Empty
                    : $" <color=#5a6974>[{meta.FilterShortcutLabel}]</color>";
                lines.Add(PresentationLine.Header($"<color=#cfd6dd><b>{meta.HeaderLabel}</b></color>{shortCut}"));

                foreach (InventoryViewModel.Row raw in inCat)
                {
                    char letter = InventoryViewModel.LetterForIndex(orderedItems.Count);
                    InventoryViewModel.Row tagged = raw.WithLetter(letter);
                    orderedItems.Add(tagged);
                    lines.Add(PresentationLine.Item(tagged));
                }
            }
        }

        static bool IsFavorite(InventoryViewModel.Row r) =>
            r.Instance != null && (r.Instance.UserMarks & ItemUserMark.Favorite) != 0;

        static bool RowMatchesSearch(InventoryViewModel.Row r, string needle)
        {
            if (r.Item == null)
                return false;

            if (r.Item.itemName.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            return false;
        }
    }
}
