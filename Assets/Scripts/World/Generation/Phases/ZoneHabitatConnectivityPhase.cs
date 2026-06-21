using JRogue.Manager.Map;
using JRogue.World.Generation.Phases;
using JRogue.World.Generation.Zones;

namespace JRogue.World.Generation.Phases
{
    /// <summary>
    /// Ensures all habitat zones are walkable-reachable from the player start; retries proc fill if needed.
    /// </summary>
    public sealed class ZoneHabitatConnectivityPhase : IDungeonGenerationPhase
    {
        const int MaxFillAttempts = 8;

        public void Execute(DungeonGenerationContext context)
        {
            MapManager map = MapManager.Instance;
            if (map == null)
            {
                DungeonGenerationLog.Error($"{nameof(ZoneHabitatConnectivityPhase)}: MapManager.Instance is null.");
                return;
            }

            for (int attempt = 0; attempt < MaxFillAttempts; attempt++)
            {
                context.ZoneFillAttempt = attempt;
                if (attempt > 0)
                {
                    new ZoneFillPhase().Execute(context);
                    new ZoneBoundaryPhase().Execute(context);
                }

                if (ZoneHabitatConnectivityEnforcer.TryEnsureAllHabitatZonesReachable(
                        context,
                        map,
                        out int carvedCells,
                        out string summary))
                {
                    if (attempt > 0 || carvedCells > 0)
                    {
                        DungeonGenerationLog.Phase(
                            nameof(ZoneHabitatConnectivityPhase),
                            $"attempt={attempt + 1} carved={carvedCells} {summary}");
                    }

                    ZoneGenerationDiagnostics.LogCheckpoint(context, "after ZoneHabitatConnectivityPhase");
                    return;
                }

                DungeonGenerationLog.Warn(
                    $"{nameof(ZoneHabitatConnectivityPhase)} attempt {attempt + 1}/{MaxFillAttempts} failed — {summary}");
            }

            DungeonGenerationLog.Error(
                $"{nameof(ZoneHabitatConnectivityPhase)} could not connect habitat zones after {MaxFillAttempts} attempts.");
            ZoneGenerationDiagnostics.LogCheckpoint(context, "after ZoneHabitatConnectivityPhase (failed)");
        }
    }
}
