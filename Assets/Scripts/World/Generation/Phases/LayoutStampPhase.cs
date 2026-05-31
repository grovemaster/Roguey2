using JRogue.Manager.Map;
using JRogue.World.Generation;
using UnityEngine;

namespace JRogue.World.Generation.Phases
{
    public sealed class LayoutStampPhase : IDungeonGenerationPhase
    {
        public void Execute(DungeonGenerationContext context)
        {
            DungeonFloorDefinition def = context.Definition;
            DungeonLayoutStamp stamp = def?.LayoutStamp;
            if (stamp == null)
            {
                DungeonGenerationLog.Error("LayoutStampPhase: missing layoutStamp on floor definition.");
                return;
            }

            MapManager map = MapManager.Instance;
            if (map == null)
            {
                DungeonGenerationLog.Error("LayoutStampPhase: MapManager.Instance is null.");
                return;
            }

            if (map.FloorMap == null || map.WallMap == null)
            {
                DungeonGenerationLog.Error("LayoutStampPhase: floor/wall tilemaps not bound — call BindToMapManager before generate.");
                return;
            }

            Debug.Log(
                $"[TileDebug] LayoutStampPhase floorId={def.FloorId} " +
                $"instanceFloorId={context.Instance?.FloorId} " +
                $"mapFloorId={map.FloorMap?.GetInstanceID()} anchor={map.FloorMap?.tileAnchor} " +
                $"instanceFloorMapId={context.Instance?.Tilemaps.FloorMap?.GetInstanceID()} " +
                $"sameRef={ReferenceEquals(map.FloorMap, context.Instance?.Tilemaps.FloorMap)}");

            map.ConfigurePaintTiles(def.FloorTile, def.WallTile);
            map.PaintLayoutStamp(stamp);
            context.PlayerStart = stamp.PlayerStart;
            context.AddChebyshevDisk(context.PlayerStart, def.PlayerSafeRadius);
            DungeonGenerationLog.Phase(nameof(LayoutStampPhase),
                $"painted {stamp.Width}x{stamp.Height} playerStart={context.PlayerStart}");
        }
    }
}
