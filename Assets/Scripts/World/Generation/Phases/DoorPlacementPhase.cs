namespace JRogue.World.Generation.Phases
{
    /// <summary>
    /// Applies <see cref="DungeonFloorDefinition.DoorPolicy"/> (procedural doors deferred).
    /// </summary>
    public sealed class DoorPlacementPhase : IDungeonGenerationPhase
    {
        public void Execute(DungeonGenerationContext context)
        {
            DungeonFloorDefinition def = context.Definition;
            if (def == null)
                return;

            switch (def.DoorPolicy)
            {
                case DungeonDoorPolicy.None:
                    DungeonGenerationLog.Phase(nameof(DoorPlacementPhase), "policy=None");
                    return;
                case DungeonDoorPolicy.StampOnly:
                case DungeonDoorPolicy.VaultOnly:
                    DungeonGenerationLog.Phase(nameof(DoorPlacementPhase),
                        $"policy={def.DoorPolicy} — doors from stamp/vault authoring only");
                    return;
                case DungeonDoorPolicy.Procedural:
                    DungeonGenerationLog.Warn(
                        $"{nameof(DoorPlacementPhase)}: Procedural door placement not implemented; use stamp markers or vaults.");
                    return;
            }
        }
    }
}
