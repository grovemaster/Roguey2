using JRogue.World.Generation.MonsterSpawn;

namespace JRogue.World.Generation.Phases
{
    public sealed class MonsterSpawnSchedulePhase : IDungeonGenerationPhase
    {
        public void Execute(DungeonGenerationContext context)
        {
            if (context?.Instance == null || context.Definition == null)
                return;

            int dungeonDay = MonsterSpawnScheduleService.GetCurrentDungeonDay();
            MonsterSpawnScheduleService.ApplyForDungeonDay(
                context.Instance,
                dungeonDay,
                context.RunSeed);
        }
    }
}
