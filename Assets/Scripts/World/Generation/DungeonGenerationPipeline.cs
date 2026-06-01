using System.Collections.Generic;
using JRogue.World.Generation.Phases;
using UnityEngine;

namespace JRogue.World.Generation
{
    /// <summary>
    /// Ordered first-visit generation phases for v0b (see Dynamic-Dungeon-Floor-Generation-Requirements §10).
    /// </summary>
    public static class DungeonGenerationPipeline
    {
        static readonly IDungeonGenerationPhase[] FirstVisitPhases =
        {
            new LayoutStampPhase(),
            new VaultPlacementPhase(),
            new PortalPlacementPhase(),
            new PortalSetupPhase(),
            new TownPortalSetupPhase(),
            new LightingInitPhase(),
            new DoorPlacementPhase(),
            new HazardPopulationPhase(),
            new TrapPopulationPhase(),
            new FloorItemPopulationPhase(),
            new InteractablePopulationPhase(),
            new EnemyPopulationPhase(),
        };

        public static IReadOnlyList<IDungeonGenerationPhase> Phases => FirstVisitPhases;

        public static void GenerateFirstVisit(DungeonFloorInstance instance, int runSeed)
        {
            if (instance == null || instance.IsGenerated)
                return;

            DungeonFloorDefinition def = instance.Definition;
            if (def == null)
            {
                DungeonGenerationLog.Error("Missing floor definition on instance.");
                return;
            }

            DungeonGenerationLog.Info($"GenerateFirstVisit floorId={def.FloorId} seed={runSeed}");

            int floorSalt = def.FloorId != null ? def.FloorId.GetHashCode() : 0;
            var context = new DungeonGenerationContext(def, instance, runSeed, floorSalt);
            context.PlayerStart = def.LayoutStamp != null
                ? def.LayoutStamp.PlayerStart
                : Vector3Int.zero;

            PartyFormationSpawnProfile profile = def.FormationProfile;
            if (profile != null && profile.TryGetOffsetsForCount(1, out Vector3Int[] offsets))
            {
                var formationCells = new List<Vector3Int> { context.PlayerStart + offsets[0] };
                context.BuildSafeZone(formationCells, def.PlayerSafeRadius);
            }
            else
            {
                context.BuildSafeZone(new[] { context.PlayerStart }, def.PlayerSafeRadius);
            }

            RunPhases(context);
            instance.MarkGenerated(context.PlayerStart, context.PortalArrivals);
            DungeonFloorServiceBinder.CaptureFeatureState(instance);
            DungeonGenerationLog.Info(
                $"GenerateFirstVisit complete playerStart={context.PlayerStart} portals={context.Portals.Count}");
        }

        public static void RunPhases(DungeonGenerationContext context)
        {
            for (int i = 0; i < FirstVisitPhases.Length; i++)
            {
                string phaseName = FirstVisitPhases[i].GetType().Name;
                DungeonGenerationLog.Phase(phaseName, "begin");
                FirstVisitPhases[i].Execute(context);
                DungeonGenerationLog.Phase(phaseName, "done");
            }
        }
    }
}
