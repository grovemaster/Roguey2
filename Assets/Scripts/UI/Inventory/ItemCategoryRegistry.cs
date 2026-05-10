using System;
using System.Collections.Generic;
using System.Linq;
using JRogue.Item;
using UnityEngine;

namespace JRogue.UI.Inventory
{
    /// <summary>
    /// Authoritative UX metadata per <see cref="ItemCategory"/>. Unknown future enum members fall back to
    /// Misc-style defaults (sorted last, sensible header label)—no brittle switch exhaustion.
    /// </summary>
    public readonly struct ItemCategoryUiInfo
    {
        public ItemCategoryUiInfo(int sortOrder, string headerLabel, string filterShortcutLabel)
        {
            SortOrder = sortOrder;
            HeaderLabel = headerLabel ?? "Misc";
            FilterShortcutLabel = filterShortcutLabel;
        }

        public int SortOrder { get; }

        /// <summary>Shown as list section header.</summary>
        public string HeaderLabel { get; }

        /// <summary>Optional single-character hint printed in brackets for filter cycling help text.</summary>
        public string FilterShortcutLabel { get; }
    }

    public static class ItemCategoryRegistry
    {
        const int FallbackOrderOffset = 10_000;

        static readonly Dictionary<ItemCategory, ItemCategoryUiInfo> Known =
            BuildKnown();

        static Dictionary<ItemCategory, ItemCategoryUiInfo> BuildKnown()
        {
            int o = 0;
            ItemCategoryUiInfo R(ItemCategory cat, string label, string key = null)
            {
                o += 10;
                return new ItemCategoryUiInfo(o, label, key);
            }

            return new Dictionary<ItemCategory, ItemCategoryUiInfo>
            {
                { ItemCategory.Weapon, R(ItemCategory.Weapon, "Weapons", "W") },
                { ItemCategory.Missile, R(ItemCategory.Missile, "Missiles", "M") },
                { ItemCategory.Armor, R(ItemCategory.Armor, "Armor", "A") },
                { ItemCategory.Accessory, R(ItemCategory.Accessory, "Accessories", "y") },
                { ItemCategory.Wand, R(ItemCategory.Wand, "Wands", "n") },
                { ItemCategory.Staff, R(ItemCategory.Staff, "Staves", "f") },
                { ItemCategory.Potion, R(ItemCategory.Potion, "Potions", "P") },
                { ItemCategory.Scroll, R(ItemCategory.Scroll, "Scrolls", "S") },
                { ItemCategory.Spellbook, R(ItemCategory.Spellbook, "Spellbooks", "b") },
                { ItemCategory.Book, R(ItemCategory.Book, "Books", "B") },
                { ItemCategory.Evocable, R(ItemCategory.Evocable, "Evocables", "E") },
                { ItemCategory.Essence, R(ItemCategory.Essence, "Essences", "e") },
                { ItemCategory.Treasure, R(ItemCategory.Treasure, "Treasures", "T") },
                { ItemCategory.Artifact, R(ItemCategory.Artifact, "Artifacts", "r") },
                { ItemCategory.Relic, R(ItemCategory.Relic, "Relics", "l") },
                { ItemCategory.Currency, R(ItemCategory.Currency, "Currency", "C") },
                { ItemCategory.Junk, R(ItemCategory.Junk, "Junk", "J") },
                { ItemCategory.QuestItem, R(ItemCategory.QuestItem, "Quest", "Q") },
                { ItemCategory.PlotItem, R(ItemCategory.PlotItem, "Plot / Key", "K") },
            };
        }

        public static ItemCategoryUiInfo Get(ItemCategory category)
        {
            return Known.TryGetValue(category, out ItemCategoryUiInfo info)
                ? info
                : Fallback(category);
        }

        static ItemCategoryUiInfo Fallback(ItemCategory category)
        {
            string name = Enum.GetName(typeof(ItemCategory), category) ?? "Misc";
            string spaced = RegexifyEnumName(name);
            int ordinal = (int)category;
            return new ItemCategoryUiInfo(FallbackOrderOffset + ordinal, spaced + " · Misc bucket", string.Empty);
        }

        /// <summary>Human-ish split for unknown enum literals (e.g. TwoWords → Two Words).</summary>
        static string RegexifyEnumName(string enumName)
        {
            return enumName.Replace("_", " ");
        }

        /// <summary>Iterate categories sorted for UI/filter cycling. Currency omitted (handled by ledger strip).</summary>
        public static IReadOnlyList<ItemCategory> CategoriesForFilterCycle()
        {
            return Enum.GetValues(typeof(ItemCategory)).Cast<ItemCategory>()
                .Where(c => c != ItemCategory.Currency)
                .OrderBy(c => Get(c).SortOrder)
                .ThenBy(c => c.ToString())
                .ToArray();
        }
    }
}
