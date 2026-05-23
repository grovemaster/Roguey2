using UnityEngine;

namespace JRogue.Item
{
    [CreateAssetMenu(fileName = "ManaStone", menuName = "JRogue/Item/Mana Stone")]
    public class ManaStoneItemData : ItemData
    {
        [Range(1, 9)]
        public int tier = 9;

        [Tooltip("When true, party members auto-collect this stone on tile entry.")]
        public bool autoPickupOnStep = true;

        void OnValidate()
        {
            category = ItemCategory.Currency;
            weight = 0f;
            requiresAppraisal = false;
            goldValue = 0;
            tier = Mathf.Clamp(tier, 1, 9);
        }
    }
}
