using System;
using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Item;
using JRogue.Manager.Equipment;
using JRogue.Manager.Inventory;
using UnityEngine;

namespace JRogue.UI.Inventory
{
    /// <summary>
    /// Read-only snapshot for inventory UI. One row per physical <see cref="ItemInstance"/> (no merging
    /// unrelated drops that share the same <see cref="ItemData"/> asset).
    /// </summary>
    public sealed class InventoryViewModel
    {
        public readonly struct Row
        {
            public Row(
                char letter,
                ItemInstance instance,
                BaseActor owner,
                string ownerDisplayName,
                bool isEquipped,
                EquipmentSlot? equippedSlot,
                int carriedListIndex,
                float stackedWeight)
            {
                Letter = letter;
                Instance = instance;
                Owner = owner;
                OwnerDisplayName = ownerDisplayName;
                IsEquipped = isEquipped;
                EquippedSlot = equippedSlot;
                CarriedListIndex = carriedListIndex;
                StackedWeight = stackedWeight;
            }

            public char Letter { get; }
            public ItemInstance Instance { get; }
            public BaseActor Owner { get; }
            public string OwnerDisplayName { get; }
            public bool IsEquipped { get; }
            public EquipmentSlot? EquippedSlot { get; }
            public int CarriedListIndex { get; }
            public float StackedWeight { get; }

            public ItemData Item => Instance != null ? Instance.Definition : null;

            public Row WithLetter(char letter) =>
                new Row(
                    letter,
                    Instance,
                    Owner,
                    OwnerDisplayName,
                    IsEquipped,
                    EquippedSlot,
                    CarriedListIndex,
                    StackedWeight);
        }

        readonly List<Row> _rows;

        InventoryViewModel(List<Row> rows) => _rows = rows;

        public IReadOnlyList<Row> Rows => _rows;

        /// <summary>All party members: equipped rows (per slot order) then bag rows.</summary>
        public static InventoryViewModel BuildPartyAggregate(IReadOnlyList<BaseActor> partyMembers)
        {
            var raw = new List<Row>();

            if (partyMembers == null)
                return new InventoryViewModel(raw);

            for (int p = 0; p < partyMembers.Count; p++)
            {
                BaseActor member = partyMembers[p];
                if (member == null || !member.gameObject.activeInHierarchy)
                    continue;

                string label = member.DisplayName;
                EquipmentManager eq = member.GetComponent<EquipmentManager>();
                InventoryManager inv = member.GetComponent<InventoryManager>();

                if (eq != null)
                {
                    foreach (EquipmentSlot slot in (EquipmentSlot[])Enum.GetValues(typeof(EquipmentSlot)))
                    {
                        ItemInstance equipped = eq.GetEquippedInstance(slot);
                        ItemData defEq = equipped?.Definition;
                        if (defEq == null || defEq.category == ItemCategory.Currency)
                            continue;

                        raw.Add(new Row(
                            '?',
                            equipped,
                            member,
                            label,
                            isEquipped: true,
                            equippedSlot: slot,
                            carriedListIndex: -1,
                            stackedWeight: equipped.TotalWeight));
                    }
                }

                if (inv != null)
                {
                    IReadOnlyList<ItemInstance> bag = inv.CarriedItems;
                    for (int i = 0; i < bag.Count; i++)
                    {
                        ItemInstance it = bag[i];
                        ItemData defBag = it?.Definition;
                        if (defBag == null || defBag.category == ItemCategory.Currency)
                            continue;

                        raw.Add(new Row(
                            '?',
                            it,
                            member,
                            label,
                            isEquipped: false,
                            equippedSlot: null,
                            carriedListIndex: i,
                            stackedWeight: it.TotalWeight));
                    }
                }
            }

            for (int i = 0; i < raw.Count; i++)
            {
                Row r = raw[i];
                raw[i] = new Row(
                    LetterForIndex(i),
                    r.Instance,
                    r.Owner,
                    r.OwnerDisplayName,
                    r.IsEquipped,
                    r.EquippedSlot,
                    r.CarriedListIndex,
                    r.StackedWeight);
            }

            return new InventoryViewModel(raw);
        }

        /// <summary>
        /// Equipped + carried for a single actor. Currency defs are omitted (shown only on the party ledger strip).
        /// </summary>
        public static InventoryViewModel BuildPartyMember(IReadOnlyList<BaseActor> partyMembers, BaseActor member)
        {
            var filtered = new List<Row>();

            if (partyMembers == null || member == null || !member.gameObject.activeInHierarchy)
                return new InventoryViewModel(filtered);

            string label = member.DisplayName;

            EquipmentManager eq = member.GetComponent<EquipmentManager>();
            InventoryManager inv = member.GetComponent<InventoryManager>();

            if (eq != null)
            {
                foreach (EquipmentSlot slot in (EquipmentSlot[])Enum.GetValues(typeof(EquipmentSlot)))
                {
                    ItemInstance equipped = eq.GetEquippedInstance(slot);
                    ItemData def = equipped?.Definition;
                    if (def == null || def.category == ItemCategory.Currency)
                        continue;

                    filtered.Add(new Row(
                        '?',
                        equipped,
                        member,
                        label,
                        isEquipped: true,
                        equippedSlot: slot,
                        carriedListIndex: -1,
                        stackedWeight: equipped.TotalWeight));
                }
            }

            if (inv != null)
            {
                IReadOnlyList<ItemInstance> bag = inv.CarriedItems;
                for (int i = 0; i < bag.Count; i++)
                {
                    ItemInstance it = bag[i];
                    ItemData def = it?.Definition;
                    if (def == null || def.category == ItemCategory.Currency)
                        continue;

                    filtered.Add(new Row(
                        '?',
                        it,
                        member,
                        label,
                        isEquipped: false,
                        equippedSlot: null,
                        carriedListIndex: i,
                        stackedWeight: it.TotalWeight));
                }
            }

            for (int i = 0; i < filtered.Count; i++)
            {
                Row r = filtered[i];
                filtered[i] = r.WithLetter(LetterForIndex(i));
            }

            return new InventoryViewModel(filtered);
        }

        public static char LetterForIndex(int i)
        {
            if (i < 26) return (char)('a' + i);
            if (i < 52) return (char)('A' + (i - 26));
            return '?';
        }
    }
}
