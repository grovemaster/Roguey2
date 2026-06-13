using System.Collections.Generic;
using JRogue.Ability;
using JRogue.Item.Essence;
using UnityEngine;

namespace JRogue.Racial
{
    [CreateAssetMenu(fileName = "CyborgImplant", menuName = "JRogue/Racial/Cyborg Implant")]
    public class CyborgImplantDefinition : ScriptableObject, IRacialProgressionPayload
    {
        public string implantId;
        public string displayName;
        [TextArea] public string description;

        public List<ImplantSlot> allowedSlots = new List<ImplantSlot>();

        public CyborgImplantInstallCost installCost;
        public CyborgImplantRemoveCost removeCost;

        [Header("Racial benefits & restrictions (progression node)")]
        public List<RacialRestrictionDefinition> racialRestrictions = new List<RacialRestrictionDefinition>();
        public List<RacialBenefitDefinition> racialBenefits = new List<RacialBenefitDefinition>();

        [Header("Stat modifications")]
        public List<AttributeModifier> statModifiers = new List<AttributeModifier>();
        public List<DamageResistanceModifier> resistanceModifiers = new List<DamageResistanceModifier>();

        [Header("Passive & active abilities")]
        public List<PassiveEffect> passiveEffects = new List<PassiveEffect>();
        public List<AbilityAction> activeAbilities = new List<AbilityAction>();

        public IReadOnlyList<RacialRestrictionDefinition> RacialRestrictions => racialRestrictions;
        public IReadOnlyList<RacialBenefitDefinition> RacialBenefits => racialBenefits;
        public IReadOnlyList<AttributeModifier> StatModifiers => statModifiers;
        public IReadOnlyList<DamageResistanceModifier> ResistanceModifiers => resistanceModifiers;
        public IReadOnlyList<PassiveEffect> PassiveEffects => passiveEffects;
        public IReadOnlyList<AbilityAction> ActiveAbilities => activeAbilities;

        public bool IsAllowedInSlot(ImplantSlot slot) =>
            allowedSlots != null && allowedSlots.Contains(slot);

        public bool TryGetTargetSlot(out ImplantSlot slot, out string validationError)
        {
            slot = default;
            validationError = null;

            if (allowedSlots == null || allowedSlots.Count != 1)
            {
                validationError = "allowedSlots must contain exactly one slot.";
                return false;
            }

            slot = allowedSlots[0];
            return true;
        }
    }
}
