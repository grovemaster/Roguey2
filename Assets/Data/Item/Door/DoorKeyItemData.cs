using UnityEngine;

namespace JRogue.Item
{
    [CreateAssetMenu(fileName = "DoorKey", menuName = "JRogue/Item/Door Key")]
    public sealed class DoorKeyItemData : ItemData
    {
        [Tooltip("Unlocks exactly this door id (DoorDefinition.doorId).")]
        public string targetDoorId;

        void OnValidate()
        {
            category = ItemCategory.Key;
            weight = 0.1f;
            requiresAppraisal = false;
            isThrowable = false;
        }
    }
}
