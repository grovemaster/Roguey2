using System;

namespace JRogue.UI.Hotbar
{
    [Serializable]
    public sealed class HotbarEntry
    {
        public HotbarEntryKind Kind = HotbarEntryKind.Empty;
        public int essenceSlotIndex;
        public int abilityIndex;
        public int equipmentSlot;
        public string itemInstanceId;
        public string contractInstanceId;
        public string racialBindingKey;
        public string abilityAssetName;
        public string knightNodeId;

        public bool IsEmpty() => Kind == HotbarEntryKind.Empty;

        public string EntryKey()
        {
            if (IsEmpty())
                return "empty";

            return Kind switch
            {
                HotbarEntryKind.EssenceActive =>
                    $"essence:{essenceSlotIndex}:{abilityIndex}",
                HotbarEntryKind.EquipmentActive =>
                    $"equip:{equipmentSlot}:{abilityIndex}",
                HotbarEntryKind.HumanMageSpell =>
                    $"mage:{abilityIndex}",
                HotbarEntryKind.DragonianSpell =>
                    $"dragonian:{abilityIndex}",
                HotbarEntryKind.HumanKnightSkill =>
                    $"knight:{knightNodeId ?? string.Empty}:{abilityIndex}",
                HotbarEntryKind.HumanPriestInvocation =>
                    $"priest:{abilityIndex}",
                HotbarEntryKind.RacialActive =>
                    $"racial:{racialBindingKey ?? string.Empty}",
                HotbarEntryKind.InventoryActive =>
                    $"inv-active:{itemInstanceId ?? string.Empty}:{abilityIndex}",
                HotbarEntryKind.InventoryUse =>
                    $"inv-use:{itemInstanceId ?? string.Empty}",
                HotbarEntryKind.ElementalSpiritSummon =>
                    $"spirit-summon:{contractInstanceId ?? string.Empty}",
                _ => $"unknown:{(int)Kind}",
            };
        }

        public bool EqualsEntry(HotbarEntry other)
        {
            if (other == null)
                return false;

            if (Kind != other.Kind)
                return false;

            return Kind switch
            {
                HotbarEntryKind.Empty => true,
                HotbarEntryKind.EssenceActive =>
                    essenceSlotIndex == other.essenceSlotIndex && abilityIndex == other.abilityIndex,
                HotbarEntryKind.EquipmentActive =>
                    equipmentSlot == other.equipmentSlot && abilityIndex == other.abilityIndex,
                HotbarEntryKind.HumanMageSpell =>
                    abilityIndex == other.abilityIndex,
                HotbarEntryKind.DragonianSpell =>
                    abilityIndex == other.abilityIndex,
                HotbarEntryKind.HumanKnightSkill =>
                    string.Equals(knightNodeId, other.knightNodeId, StringComparison.Ordinal)
                    && abilityIndex == other.abilityIndex,
                HotbarEntryKind.HumanPriestInvocation =>
                    abilityIndex == other.abilityIndex,
                HotbarEntryKind.RacialActive =>
                    string.Equals(racialBindingKey, other.racialBindingKey, StringComparison.Ordinal),
                HotbarEntryKind.InventoryActive =>
                    string.Equals(itemInstanceId, other.itemInstanceId, StringComparison.Ordinal)
                    && abilityIndex == other.abilityIndex,
                HotbarEntryKind.InventoryUse =>
                    string.Equals(itemInstanceId, other.itemInstanceId, StringComparison.Ordinal),
                HotbarEntryKind.ElementalSpiritSummon =>
                    string.Equals(contractInstanceId, other.contractInstanceId, StringComparison.Ordinal),
                _ => false,
            };
        }

        public HotbarEntry Clone() =>
            new HotbarEntry
            {
                Kind = Kind,
                essenceSlotIndex = essenceSlotIndex,
                abilityIndex = abilityIndex,
                equipmentSlot = equipmentSlot,
                itemInstanceId = itemInstanceId,
                contractInstanceId = contractInstanceId,
                racialBindingKey = racialBindingKey,
                abilityAssetName = abilityAssetName,
                knightNodeId = knightNodeId,
            };
    }
}
