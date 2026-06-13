using UnityEngine;

namespace JRogue.Racial
{
    /// <summary>
    /// Invokes racial loadout passive hooks without requiring <see cref="JRogue.Manager.Essence.EssenceSlotManager"/>.
    /// </summary>
    public static class RacialPassiveHooks
    {
        public static void RefreshPassives(GameObject actor)
        {
            actor.GetComponent<RacialLoadoutApplier>()?.RefreshPassives();
            actor.GetComponent<SpiritImprintRuntime>()?.RefreshPassives();
            actor.GetComponent<ElementalSpiritContractsRuntime>()?.RefreshPassives();
            actor.GetComponent<BeastmanSoulBeastRuntime>()?.RefreshPassives();
            actor.GetComponent<TieflingImplantsRuntime>()?.RefreshPassives();
            actor.GetComponent<DwarfCommonAbilitiesRuntime>()?.RefreshPassives();
            actor.GetComponent<DwarfAncestorPathRuntime>()?.RefreshPassives();
            actor.GetComponent<UndeadSkillTreeRuntime>()?.RefreshPassives();
        }

        public static void NotifyTurnStart(GameObject actor)
        {
            actor.GetComponent<RacialLoadoutApplier>()?.NotifyPassivesTurnStart();
            actor.GetComponent<SpiritImprintRuntime>()?.NotifyPassivesTurnStart();
            actor.GetComponent<ElementalSpiritContractsRuntime>()?.NotifyTurnStart();
            actor.GetComponent<BeastmanSoulBeastRuntime>()?.NotifyPassivesTurnStart();
            actor.GetComponent<TieflingImplantsRuntime>()?.NotifyPassivesTurnStart();
            actor.GetComponent<DwarfCommonAbilitiesRuntime>()?.NotifyPassivesTurnStart();
            actor.GetComponent<DwarfAncestorPathRuntime>()?.NotifyPassivesTurnStart();
            actor.GetComponent<UndeadSkillTreeRuntime>()?.NotifyPassivesTurnStart();
        }
    }
}
