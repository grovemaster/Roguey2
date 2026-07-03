using JRogue.Manager.Map;
using JRogue.World.Generation.Zones;
using UnityEngine;

namespace JRogue.World.Generation.Phases
{
    /// <summary>
    /// Runs after vault placement so playerStart respects reserved vault footprints.
    /// </summary>
    public sealed class PlayerSpawnAnchorPhase : IDungeonGenerationPhase
    {
        public void Execute(DungeonGenerationContext context)
        {
            MapManager map = MapManager.Instance;
            if (map == null)
            {
                DungeonGenerationLog.Error($"{nameof(PlayerSpawnAnchorPhase)}: MapManager.Instance is null.");
                return;
            }

            Vector3Int before = context.PlayerStart;
            if (!PlayerSpawnAnchorResolver.TryResolve(context, map))
            {
                DungeonGenerationLog.Warn(
                    $"{nameof(PlayerSpawnAnchorPhase)}: kept provisional playerStart={before}.");
                return;
            }

            if (before != context.PlayerStart)
            {
                DungeonGenerationLog.Phase(
                    nameof(PlayerSpawnAnchorPhase),
                    $"playerStart {before} -> {context.PlayerStart}");
            }
            else
            {
                DungeonGenerationLog.Phase(
                    nameof(PlayerSpawnAnchorPhase),
                    $"playerStart={context.PlayerStart} (already walkable)");
            }

            ZoneGenerationDiagnostics.LogCheckpoint(context, "after PlayerSpawnAnchorPhase");
        }
    }
}
