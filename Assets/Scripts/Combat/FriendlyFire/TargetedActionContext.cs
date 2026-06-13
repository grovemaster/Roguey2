using JRogue.Ability;
using JRogue.Input;
using UnityEngine;

namespace JRogue.Combat.FriendlyFire
{
    /// <summary>Snapshot of a pending player targeted action for friendly-fire preview.</summary>
    public struct TargetedActionContext
    {
        public PlayerAbilitySource Source;
        public int SlotIndex;
        public int AbilityIndex;
        public AbilityAction InventoryAbility;

        public static TargetedActionContext FromInventory(AbilityAction ability) =>
            new TargetedActionContext
            {
                Source = PlayerAbilitySource.InventoryItem,
                InventoryAbility = ability,
            };

        public static TargetedActionContext FromEssence(int slotIndex, int abilityIndex) =>
            new TargetedActionContext
            {
                Source = PlayerAbilitySource.Essence,
                SlotIndex = slotIndex,
                AbilityIndex = abilityIndex,
            };

        public static TargetedActionContext FromEquipment(int slotIndex, int abilityIndex) =>
            new TargetedActionContext
            {
                Source = PlayerAbilitySource.EquipmentItem,
                SlotIndex = slotIndex,
                AbilityIndex = abilityIndex,
            };

        public static TargetedActionContext FromHumanMageSpell(int abilityIndex) =>
            new TargetedActionContext
            {
                Source = PlayerAbilitySource.HumanMageSpell,
                SlotIndex = abilityIndex,
                AbilityIndex = abilityIndex,
            };

        public static TargetedActionContext FromDragonianSpell(int abilityIndex) =>
            new TargetedActionContext
            {
                Source = PlayerAbilitySource.DragonianSpell,
                SlotIndex = abilityIndex,
                AbilityIndex = abilityIndex,
            };

        public static TargetedActionContext BowAim() =>
            new TargetedActionContext { Source = PlayerAbilitySource.BowAim };

        public static TargetedActionContext FromRacial(AbilityAction ability) =>
            new TargetedActionContext
            {
                Source = PlayerAbilitySource.RacialActive,
                InventoryAbility = ability,
            };
    }
}
