using System.Collections.Generic;
using JRogue.Ability;
using JRogue.Item.Essence;
using UnityEngine;

namespace JRogue.Racial
{
    [CreateAssetMenu(fileName = "CyborgImplant", menuName = "JRogue/Racial/Cyborg Implant")]
    public class CyborgImplantDefinition : ScriptableObject
    {
        public string implantId;
        public string displayName;
        [TextArea] public string description;

        public List<ImplantSlot> allowedSlots = new List<ImplantSlot>();

        public List<AttributeModifier> statModifiers;
        public List<DamageResistanceModifier> resistanceModifiers;
        public List<PassiveEffect> passiveEffects;
        public List<AbilityAction> activeAbilities;

        public bool IsAllowedInSlot(ImplantSlot slot) =>
            allowedSlots != null && allowedSlots.Contains(slot);
    }
}
