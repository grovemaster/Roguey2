using UnityEngine;
using JRogue.Stats;

namespace JRogue.Item.Effect
{
    [CreateAssetMenu(fileName = "New Stat Modifier", menuName = "JRogue/Effects/Stat Modifier")]
    public class StatModifierEffect : ItemEffect
    {
        // public enum StatType { Strength, Dexterity, Constitution, Luck }

        public StatType targetStat;
        public int modifierAmount;

        public override void OnEquip(GameObject user)
        {
            Debug.Log("Executing onEquip()");
            CharacterStats stats = user.GetComponent<CharacterStats>();
            if (stats == null) return;

            // // Apply the modifier to the correct Stat object
            // switch (targetStat)
            // {
            //     case StatType.Strength: stats.Strength.AddModifier(modifierAmount); break;
            //     case StatType.Dexterity: stats.Dexterity.AddModifier(modifierAmount); break;
            //     case StatType.Constitution: stats.Constitution.AddModifier(modifierAmount); break;
            //     case StatType.Luck: stats.Luck.AddModifier(modifierAmount); break;
            // }

            // Debug.Log($"Applied {modifierAmount} to {targetStat}. New Value: {stats.Strength.GetValue()}");

            // Use our helper to find the stat and add the modifier
            // 'this' refers to the ScriptableObject itself, acting as the unique source ID
            Stat statToModify = stats.GetStatByType(targetStat);

            if (statToModify != null)
            {
                statToModify.AddModifier(modifierAmount, this);
                Debug.Log($"Applied {modifierAmount} to {targetStat}. New Value: {statToModify.GetValue()}");
            }
        }

        public override void OnUnequip(GameObject user)
        {
            Debug.Log("Executing onUnEquip()");
            CharacterStats stats = user.GetComponent<CharacterStats>();
            if (stats == null) return;

            // Remove the modifier when the item is taken off
            // switch (targetStat)
            // {
            //     case StatType.Strength: stats.Strength.RemoveModifier(modifierAmount); break;
            //     case StatType.Dexterity: stats.Dexterity.RemoveModifier(modifierAmount); break;
            //     case StatType.Constitution: stats.Constitution.RemoveModifier(modifierAmount); break;
            //     case StatType.Luck: stats.Luck.RemoveModifier(modifierAmount); break;
            // }

            Stat statToModify = stats.GetStatByType(targetStat);

            if (statToModify != null)
            {
                // Use the new source-based removal to ensure we only take back what we added
                statToModify.RemoveModifiersFromSource(this);
            }
        }
    }
}