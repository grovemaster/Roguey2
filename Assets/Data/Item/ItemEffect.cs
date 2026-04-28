using UnityEngine;

namespace JRogue.Item
{
    public abstract class ItemEffect : ScriptableObject
    {
        [TextArea] public string effectDescription;

        // Called when the item is moved into an equipment slot
        public virtual void OnEquip(GameObject user) { }

        // Called when the item is removed from an equipment slot
        public virtual void OnUnequip(GameObject user) { }

        // Called when the item is "Used" or "Activated" (Potions, Spells, Active Armor)
        public virtual void OnActivate(GameObject user) { }
    }
}