namespace JRogue.World.Generation
{
    public interface IDungeonGenerationPhase
    {
        void Execute(DungeonGenerationContext context);
    }
}
