namespace JRogue.World.Generation.Phases
{
    /// <summary>
    /// v0b stub — vault stamping deferred until <see cref="DungeonVaultDefinition"/> assets exist.
    /// </summary>
    public sealed class VaultPlacementPhase : IDungeonGenerationPhase
    {
        public void Execute(DungeonGenerationContext context)
        {
            DungeonFloorDefinition def = context.Definition;
            if (def?.Vaults == null || def.Vaults.Count == 0)
                return;

            DungeonGenerationLog.Warn(
                $"{nameof(VaultPlacementPhase)}: {def.Vaults.Count} vault(s) on '{def.FloorId}' — placement not implemented yet.");
        }
    }
}
