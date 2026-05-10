using System;
using System.Collections.Generic;
using System.Linq;
using JRogue.Item;

namespace JRogue.UI.Inventory
{
    /// <summary>
    /// Filters an <see cref="InventoryViewModel"/> and builds category-grouped presentation lines plus a flat lettered row list for selection.
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
            bool inCombat)
        {
            var lines = new List<PresentationLine>();
            var orderedItems = new List<InventoryViewModel.Row>();

            if (source == null)
                return new InventoryPresentationModel(lines, orderedItems);

            IEnumerable<InventoryViewModel.Row> q = source.Rows.Where(r => r.Item != null);

            if (categoryFilter.HasValue)
                q = q.Where(r => r.Item.category == categoryFilter.Value);

            if (!string.IsNullOrWhiteSpace(searchNeedle))
            {
                string n = searchNeedle.Trim();
                q = q.Where(r =>
                    r.Item.itemName.IndexOf(n, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            if (usableOnly)
                q = q.Where(r => InventoryUsability.AppearsUsableNow(r, inCombat));

            List<InventoryViewModel.Row> working = q
                .OrderBy(r => r.Item.itemName ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .ToList();

            IGrouping<ItemCategory, InventoryViewModel.Row>[] grouped = working
                .GroupBy(r => r.Item.category)
                .OrderBy(g => ItemCategoryRegistry.Get(g.Key).SortOrder)
                .ThenBy(g => g.Key.ToString())
                .ToArray();

            foreach (IGrouping<ItemCategory, InventoryViewModel.Row> g in grouped)
            {
                List<InventoryViewModel.Row> inCat =
                    g.OrderBy(r => r.Item.itemName ?? string.Empty, StringComparer.OrdinalIgnoreCase).ToList();
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

            return new InventoryPresentationModel(lines, orderedItems);
        }
    }
}
