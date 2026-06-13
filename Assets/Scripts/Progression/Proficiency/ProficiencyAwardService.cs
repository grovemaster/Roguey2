using JRogue.Actors;
using JRogue.Racial;

namespace JRogue.Progression.Proficiency
{
    public static class ProficiencyAwardService
    {
        public static void AwardMageSpellCast(BaseActor actor, HumanMageSpellsRuntime runtime, int equippedIndex)
        {
            if (actor == null || runtime == null)
                return;

            MageSpellDefinition spell = runtime.GetEquippedSpell(equippedIndex);
            if (spell == null)
                return;

            ProficiencyResolvedAction action =
                ProficiencyStrikePayloadBuilder.FromMageSpellCast(spell, spell.ability);
            ProficiencyXpDispatcher.Dispatch(actor, action);
        }

        public static void AwardDragonianSpellCast(
            BaseActor actor,
            DragonianSpellsRuntime runtime,
            int memorizedIndex)
        {
            if (actor == null || runtime == null)
                return;

            DragonianSpellDefinition spell = runtime.GetMemorizedSpell(memorizedIndex);
            if (spell == null)
                return;

            ProficiencyResolvedAction action =
                ProficiencyStrikePayloadBuilder.FromDragonianSpellCast(spell, spell.ability);
            ProficiencyXpDispatcher.Dispatch(actor, action);
        }
    }
}
