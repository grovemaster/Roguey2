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
        static readonly IDungeonGenerationPhase[] SharedTailPhases =
        {
            new VaultPlacementPhase(),
            new PortalPlacementPhase(),
            new PortalSetupPhase(),
            new TownPortalSetupPhase(),
            new TownTimeLeverSetupPhase(),
            new TownNpcSetupPhase(),
            new TownTorchSetupPhase(),
            new LightingInitPhase(),
            new DoorPlacementPhase(),
            new HazardPopulationPhase(),
            new TrapPopulationPhase(),
            new FloorItemPopulationPhase(),
            new InteractablePopulationPhase(),
            new EnemyPopulationPhase(),
        };

        public static IReadOnlyList<IDungeonGenerationPhase> PhasesFor(DungeonFloorDefinition def)
        {
            if (def != null && def.LayoutMode == FloorLayoutMode.ZoneComposite)
                return ZoneCompositePhases;

            return PreBakedPhases;
        }

        static readonly IDungeonGenerationPhase[] PreBakedPhases = BuildPreBakedPhases();
        static readonly IDungeonGenerationPhase[] ZoneCompositePhases = BuildZoneCompositePhases();

        static IDungeonGenerationPhase[] BuildPreBakedPhases()
        {
            var phases = new IDungeonGenerationPhase[1 + SharedTailPhases.Length];
            phases[0] = new LayoutStampPhase();
            for (int i = 0; i < SharedTailPhases.Length; i++)
                phases[i + 1] = SharedTailPhases[i];
            return phases;
        }

        static IDungeonGenerationPhase[] BuildZoneCompositePhases()
        {
            var phases = new IDungeonGenerationPhase[2 + SharedTailPhases.Length];
            phases[0] = new ZoneLayoutPhase();
            phases[1] = new ZoneFillPhase();
            for (int i = 0; i < SharedTailPhases.Length; i++)
                phases[i + 2] = SharedTailPhases[i];
            return phases;
        }

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

            DungeonGenerationLog.Info($"GenerateFirstVisit floorId={def.FloorId} seed={runSeed} layout={def.LayoutMode}");

            int floorSalt = def.FloorId != null ? def.FloorId.GetHashCode() : 0;
            var context = new DungeonGenerationContext(def, instance, runSeed, floorSalt);

            if (def.LayoutMode != FloorLayoutMode.ZoneComposite)
            {
                context.PlayerStart = def.LayoutStamp != null
                    ? def.LayoutStamp.PlayerStart
                    : Vector3Int.zero;
                context.BuildSafeZoneForFloor(def);
            }

            RunPhases(context, PhasesFor(def));
            instance.MarkGenerated(
                context.PlayerStart,
                context.PortalArrivals,
                context.ZoneCellMap,
                context.ResolvedZonePieces);
            DungeonFloorServiceBinder.CaptureFeatureState(instance);
            DungeonGenerationLog.Info(
                $"GenerateFirstVisit complete playerStart={context.PlayerStart} portals={context.Portals.Count}");
        }

        public static void RunPhases(DungeonGenerationContext context)
        {
            RunPhases(context, PhasesFor(context.Definition));
        }

        public static void RunPhases(DungeonGenerationContext context, IReadOnlyList<IDungeonGenerationPhase> phases)
        {
            for (int i = 0; i < phases.Count; i++)
            {
                string phaseName = phases[i].GetType().Name;
                DungeonGenerationLog.Phase(phaseName, "begin");
                phases[i].Execute(context);
                DungeonGenerationLog.Phase(phaseName, "done");
            }
        }
    }
}
