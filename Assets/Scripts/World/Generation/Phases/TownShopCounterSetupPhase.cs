using JRogue.World.Town;
using UnityEngine;

namespace JRogue.World.Generation.Phases
{
    /// <summary>Registers shop counter cells for movement blocking and counter-talk geometry.</summary>
    public sealed class TownShopCounterSetupPhase : IDungeonGenerationPhase
    {
        public void Execute(DungeonGenerationContext context)
        {
            DungeonFloorDefinition def = context?.Definition;
            if (def == null)
                return;

            if (def.FloorId != AdventureGuildExchangeLayout.InteriorFloorId)
                return;

            ShopCounterService.EnsureAdventureGuildExchangeCounters();
            DungeonGenerationLog.Phase(
                nameof(TownShopCounterSetupPhase),
                "registered adventure guild counter cells");
        }
    }
}
