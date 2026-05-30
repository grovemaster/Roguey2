using JRogue.Ability;
using UnityEngine;

namespace JRogue.Item
{
    [CreateAssetMenu(fileName = "Evocable", menuName = "JRogue/Item/Evocable")]
    public class EvocableItemData : ItemData
    {
        public const int DefaultRechargeIntervalPlayerPhases = 10;

        [Header("Evocable — charges")]
        [Min(1)] public int maxCharges = 2;

        [Min(0)] public int startingCharges = 2;

        [Tooltip("When true, the instance is removed from inventory after the last charge is spent.")]
        public bool consumesWhenEmpty = true;

        [Min(1)]
        [Tooltip("Player phases between +1 charge when not consumable-at-empty.")]
        public int rechargeIntervalPlayerPhases = DefaultRechargeIntervalPlayerPhases;

        [Header("Evocable — effect")]
        public AbilityAction invokeAbility;

        void OnValidate()
        {
            category = ItemCategory.Evocable;
            maxCharges = Mathf.Max(1, maxCharges);
            startingCharges = Mathf.Clamp(startingCharges, 0, maxCharges);
            rechargeIntervalPlayerPhases = Mathf.Max(1, rechargeIntervalPlayerPhases);
            if (string.IsNullOrEmpty(inventoryTargetedUseLogTag) && invokeAbility != null)
                inventoryTargetedUseLogTag = $"Evocable:{itemName}";
        }
    }
}
