using JRogue.Ability;
using JRogue.Actors;
using JRogue.Input;
using JRogue.Manager.Equipment;
using JRogue.Manager.Essence;
using JRogue.Racial;
using JRogue.Stats.Racial;

namespace JRogue.Combat.FriendlyFire
{
    public static class TargetedActionResolver
    {
        public static AbilityAction ResolveAbility(BaseActor caster, in TargetedActionContext context)
        {
            if (caster == null)
                return null;

            switch (context.Source)
            {
                case PlayerAbilitySource.InventoryItem:
                    return context.InventoryAbility;
                case PlayerAbilitySource.Essence:
                    return caster.GetComponent<EssenceSlotManager>()
                        ?.GetAbility(context.SlotIndex, context.AbilityIndex);
                case PlayerAbilitySource.EquipmentItem:
                    return caster.GetComponent<EquipmentManager>()
                        ?.GetItemAbility(context.SlotIndex, context.AbilityIndex);
                case PlayerAbilitySource.HumanMageSpell:
                    return caster.GetComponent<HumanMageSpellsRuntime>()
                        ?.GetEquippedAbility(context.AbilityIndex);
                case PlayerAbilitySource.RacialActive:
                    return context.InventoryAbility;
                default:
                    return null;
            }
        }

        public static string ResolveActionLabel(BaseActor caster, in TargetedActionContext context)
        {
            if (context.Source == PlayerAbilitySource.BowAim)
                return BowRangedCombatService.GetBowShotActionLabel(caster);

            AbilityAction ability = ResolveAbility(caster, context);
            if (ability == null)
                return "Ability";

            if (!string.IsNullOrWhiteSpace(ability.abilityName))
                return ability.abilityName.Trim();

            return ability.name;
        }
    }
}
