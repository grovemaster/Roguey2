using JRogue.Racial;
using UnityEngine;

namespace JRogue.Item
{
    [CreateAssetMenu(fileName = "SpellbookItem", menuName = "JRogue/Item/Spellbook")]
    public sealed class SpellbookItemData : ItemData
    {
        public MageSpellbookDefinition spellbook;

        void OnValidate()
        {
            category = ItemCategory.Spellbook;
            if (weight <= 0f)
                weight = 1f;
            allowUseInSafeZone = true;
            activeAbilities = null;
        }
    }
}
