using JRogue.Racial;
using UnityEngine;

namespace JRogue.Item
{
    [CreateAssetMenu(fileName = "RitualOffering", menuName = "JRogue/Item/Ritual Offering")]
    public sealed class RitualOfferingItemData : ItemData
    {
        public SoulBeastRitualOfferingDefinition ritualOffering;

        void OnValidate()
        {
            category = ItemCategory.Junk;
            requiresAppraisal = false;
            isThrowable = false;
            allowUseInSafeZone = true;
            activeAbilities = null;
        }
    }
}
