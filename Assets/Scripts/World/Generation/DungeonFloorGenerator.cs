using UnityEngine;

namespace JRogue.World.Generation
{
    /// <summary>
    /// Back-compat entry point — delegates to <see cref="DungeonGenerationPipeline"/>.
    /// </summary>
    public static class DungeonFloorGenerator
    {
        public static void GenerateFirstVisit(DungeonFloorInstance instance, int runSeed) =>
            DungeonGenerationPipeline.GenerateFirstVisit(instance, runSeed);
    }
}
