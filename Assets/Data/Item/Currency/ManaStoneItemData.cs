using UnityEngine;

namespace JRogue.Item
{
    [CreateAssetMenu(fileName = "ManaStone", menuName = "JRogue/Item/Mana Stone")]
    public class ManaStoneItemData : ItemData
    {
        [Range(1, 9)]
        public int tier = 9;

        void OnValidate()
        {
            category = ItemCategory.Currency;
            weight = 0f;
            requiresAppraisal = false;
            goldValue = 0;
            autoPickupOnStep = true;
            requiresAutoPickupConfirmation = false;
            tier = Mathf.Clamp(tier, 1, 9);
        }
    }
}
