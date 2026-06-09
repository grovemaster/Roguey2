using UnityEngine;

namespace JRogue.Item
{
    [CreateAssetMenu(fileName = "FairyStone", menuName = "JRogue/Item/Fairy Stone")]
    public sealed class FairyStoneItemData : ItemData
    {
        void OnValidate()
        {
            itemName = string.IsNullOrWhiteSpace(itemName) ? "Fairy Stone" : itemName;
            category = ItemCategory.Junk;
            buyValue = 1;
            sellValue = 0;
            weight = 0.1f;
            requiresAppraisal = false;
            isThrowable = false;
            allowUseInSafeZone = true;
            activeAbilities = null;
        }
    }
}
