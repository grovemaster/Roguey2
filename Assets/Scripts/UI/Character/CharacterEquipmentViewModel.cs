using System;
using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Item;
using JRogue.Item.Essence;
using JRogue.Manager.Equipment;
using JRogue.Manager.Essence;
using JRogue.Stats;
using JRogue.Stats.Racial;
using UnityEngine;

namespace JRogue.UI.Character
{
    public enum CharacterEquipmentSelectionKind
    {
        None,
        Equipment,
        Essence
    }

    public readonly struct CharacterEquipmentSelection : IEquatable<CharacterEquipmentSelection>
    {
        public CharacterEquipmentSelectionKind Kind { get; }
        public EquipmentSlot EquipmentSlot { get; }
        public int EssenceSlotIndex { get; }

        public static CharacterEquipmentSelection None =>
            new CharacterEquipmentSelection(CharacterEquipmentSelectionKind.None, default, -1);

        public static CharacterEquipmentSelection ForEquipment(EquipmentSlot slot) =>
            new CharacterEquipmentSelection(CharacterEquipmentSelectionKind.Equipment, slot, -1);

        public static CharacterEquipmentSelection ForEssence(int slotIndex) =>
            new CharacterEquipmentSelection(CharacterEquipmentSelectionKind.Essence, default, slotIndex);

        CharacterEquipmentSelection(CharacterEquipmentSelectionKind kind, EquipmentSlot slot, int essenceIndex)
        {
            Kind = kind;
            EquipmentSlot = slot;
            EssenceSlotIndex = essenceIndex;
        }

        public bool Equals(CharacterEquipmentSelection other) =>
            Kind == other.Kind && EquipmentSlot == other.EquipmentSlot &&
            EssenceSlotIndex == other.EssenceSlotIndex;

        public override bool Equals(object obj) => obj is CharacterEquipmentSelection o && Equals(o);

        public override int GetHashCode() => HashCode.Combine((int)Kind, EquipmentSlot, EssenceSlotIndex);
    }

    public sealed class EquipmentSlotCellModel
    {
        public EquipmentSlot Slot;
        public string Label = string.Empty;
        public ItemInstance Instance;
        public bool Occupied => Instance?.Definition != null;
    }

    public sealed class EssenceSlotCellModel
    {
        public int SlotIndex;
        public EssenceData Essence;
        public bool Occupied => Essence != null;
    }

    public sealed class CharacterEquipmentSheetModel
    {
        public BaseActor Actor;
        public List<EquipmentSlotCellModel> EquipmentSlots = new();
        public List<EssenceSlotCellModel> EssenceSlots = new();
        public List<string> PermanentLines = new();
        public bool CanGainEssences = true;
        public CharacterEquipmentSelection DefaultSelection = CharacterEquipmentSelection.None;
    }

    public static class EquipmentSlotLabels
    {
        public static string GetLabel(EquipmentSlot slot) =>
            slot switch
            {
                EquipmentSlot.MainHand => "MAIN HAND",
                EquipmentSlot.OffHand => "OFF HAND",
                EquipmentSlot.Head => "HEAD",
                EquipmentSlot.Torso => "TORSO",
                EquipmentSlot.Legs => "LEGS",
                EquipmentSlot.Feet => "FEET",
                EquipmentSlot.Accessory_MainHand => "ACC (main)",
                EquipmentSlot.Accessory_OffHand => "ACC (off)",
                EquipmentSlot.Accessory_Head => "ACC (head)",
                _ => slot.ToString()
            };
    }

    public static class CharacterEquipmentViewModel
    {
        static readonly EquipmentSlot[] AllSlots =
        {
            EquipmentSlot.Head,
            EquipmentSlot.Torso,
            EquipmentSlot.MainHand,
            EquipmentSlot.OffHand,
            EquipmentSlot.Legs,
            EquipmentSlot.Feet,
            EquipmentSlot.Accessory_Head,
            EquipmentSlot.Accessory_MainHand,
            EquipmentSlot.Accessory_OffHand
        };

        public static CharacterEquipmentSheetModel Build(BaseActor actor)
        {
            var model = new CharacterEquipmentSheetModel { Actor = actor };
            if (actor == null)
                return model;

            EquipmentManager equipment = actor.GetComponent<EquipmentManager>();
            foreach (EquipmentSlot slot in AllSlots)
            {
                model.EquipmentSlots.Add(new EquipmentSlotCellModel
                {
                    Slot = slot,
                    Label = EquipmentSlotLabels.GetLabel(slot),
                    Instance = equipment != null ? equipment.GetEquippedInstance(slot) : null
                });
            }

            EssenceSlotManager essenceSlots = actor.GetComponent<EssenceSlotManager>();
            CharacterStats stats = actor.stats;
            model.CanGainEssences = stats == null ||
                                    stats.race != Race.Human ||
                                    HumanClassRules.CanGainEssences(stats.humanClass);

            PermanentStatBoostRuntime permanent = actor.GetComponent<PermanentStatBoostRuntime>();
            if (permanent != null && permanent.HasAnyBoosts())
                permanent.CopyDisplayLines(model.PermanentLines);

            if (essenceSlots != null && model.CanGainEssences)
            {
                for (int i = 0; i < essenceSlots.totalSlots; i++)
                {
                    model.EssenceSlots.Add(new EssenceSlotCellModel
                    {
                        SlotIndex = i,
                        Essence = essenceSlots.GetEssenceInSlot(i)
                    });
                }
            }

            model.DefaultSelection = ResolveDefaultSelection(model);
            return model;
        }

        static CharacterEquipmentSelection ResolveDefaultSelection(CharacterEquipmentSheetModel model)
        {
            EquipmentSlotCellModel mainHand = model.EquipmentSlots.Find(c => c.Slot == EquipmentSlot.MainHand);
            if (mainHand != null && mainHand.Occupied)
                return CharacterEquipmentSelection.ForEquipment(EquipmentSlot.MainHand);

            foreach (EquipmentSlotCellModel cell in model.EquipmentSlots)
            {
                if (cell.Occupied)
                    return CharacterEquipmentSelection.ForEquipment(cell.Slot);
            }

            return CharacterEquipmentSelection.ForEquipment(EquipmentSlot.MainHand);
        }
    }
}
