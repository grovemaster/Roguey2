using UnityEngine;

namespace JRogue.Item
{
    [CreateAssetMenu(fileName = "BeastBlood", menuName = "JRogue/Item/Beast Blood")]
    public sealed class BeastBloodItemData : ItemData
    {
        void OnValidate()
        {
            itemName = string.IsNullOrWhiteSpace(itemName) ? "Beast Blood" : itemName;
            category = ItemCategory.Potion;
            buyValue = 2;
            sellValue = 0;
            weight = 0.2f;
            requiresAppraisal = false;
            isThrowable = false;
            allowUseInSafeZone = true;
            activeAbilities = null;
        }
    }
}
